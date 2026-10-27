using Application.Features.ChangeRequests;
using Domain.Aggregates.FeatureAggregate;
using Domain.Aggregates.ProjectAggregate;
using Domain.Aggregates.SegmentAggregate;
using BusinessMakerFramework.Shared.Abstractions.UserManagement;
using Domain.Aggregates.HistoryAggregate;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace Infrastructure;

public partial class ApplicationDbContext
{
    /// <summary>Advances configuration revisions and appends safe snapshots in the same save and transaction.</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        var entries = ChangeTracker
            .Entries()
            .Where(e => (e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) && e.Metadata.FindProperty(nameof(Feature.Revision)) != null)
            .ToArray();
        var deferred = new List<(EntityEntry Entry, ConfigurationHistory History)>();
        await using var transaction = Database.CurrentTransaction is null && entries.Any(HasTemporaryKey) ? await Database.BeginTransactionAsync(cancellationToken) : null;

        foreach (var entry in entries)
        {
            var revision = entry.Property(nameof(Feature.Revision));
            var beforeRevision = (long)(revision.OriginalValue ?? 0L);
            var before = entry.State == EntityState.Added ? null : await HistorySnapshotAsync(entry, original: true, cancellationToken);

            revision.CurrentValue = beforeRevision + 1;

            var after = entry.State == EntityState.Deleted ? null : await HistorySnapshotAsync(entry, original: false, cancellationToken);
            var id = string.Join(":", entry.Metadata.FindPrimaryKey()!.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString()));

            Guid? IdValue(string name) => entry.Metadata.FindProperty(name) is not null && entry.Property(name).CurrentValue is Guid value ? value : null;

            var historyProjectId = entry.Entity is Project project ? project.Id : IdValue(nameof(Feature.ProjectId));

            if (historyProjectId is null && IdValue(nameof(FeatureEnvironment.FeatureId)) is Guid featureId)
            {
                historyProjectId = Features.Local.FirstOrDefault(f => f.Id == featureId)?.ProjectId ?? await Features
                    .Where(f => f.Id == featureId)
                    .Select(f => (Guid?)f.ProjectId)
                    .SingleOrDefaultAsync(cancellationToken);
            }

            var history = new ConfigurationHistory
            {
                ActorId = Guid.TryParse(actor?.GetClaim(Application.Common.Services.IdentityClaimNames.Subject), out var userId) ? userId : null,
                ProjectId = historyProjectId,
                EnvironmentId = entry.Entity is Domain.Aggregates.EnvironmentAggregate.Environment environment ? environment.Id : IdValue(nameof(FeatureEnvironment.EnvironmentId)),
                ResourceId = id,
                ResourceType = entry.Metadata.ClrType.Name,
                Operation = entry.State.ToString(),
                Revision = beforeRevision + 1,
                Before = before,
                After = after
            };

            if (HasTemporaryKey(entry))
            {
                deferred.Add((entry, history));
            }
            else
            {
                ConfigurationHistory.Add(history);
            }
        }

        var count = await base.SaveChangesAsync(cancellationToken);

        foreach (var (entry, history) in deferred)
        {
            history.ResourceId = string.Join(":", entry.Metadata.FindPrimaryKey()!.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString()));
            history.After = await HistorySnapshotAsync(entry, original: false, cancellationToken);
            ConfigurationHistory.Add(history);
        }

        if (deferred.Count > 0)
        {
            count += await base.SaveChangesAsync(cancellationToken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return count;
    }

    private static bool HasTemporaryKey(EntityEntry entry) => entry.Metadata.FindPrimaryKey()!.Properties.Any(p => entry.Property(p.Name).IsTemporary);

    /// <summary>Reads the persisted before-image and captures children for restorable configuration history.</summary>
    private async Task<string> HistorySnapshotAsync(EntityEntry entry, bool original, CancellationToken ct)
    {
        var values = entry.Properties
            .Where(p => !p.Metadata.IsShadowProperty() && p.Metadata.Name is not "BusinessId" and not "Version")
            .ToDictionary(p => p.Metadata.Name, p => original ? p.OriginalValue : p.CurrentValue);
        object? configuration = null;

        switch (entry.Entity)
        {
            case Feature feature:
                var previousFeature = original ? await Features
                    .AsNoTracking()
                    .SingleAsync(f => f.Id == feature.Id, ct) : feature;
                configuration = ConfigurationChanges.Capture(previousFeature);
                break;
            case FeatureEnvironment environment:
                var previousEnvironment = original ? await FeatureEnvironments
                    .AsNoTracking()
                    .Include(e => e.Strategies)
                    .ThenInclude(s => s.Constraints)
                    .Include(e => e.Strategies)
                    .ThenInclude(s => s.Variants)
                    .Include(e => e.Variants)
                    .AsSplitQuery()
                    .SingleAsync(e => e.Id == environment.Id, ct) : environment;
                if (!original && entry.State != EntityState.Added)
                {
                    await Entry(environment)
                        .Collection(e => e.Variants)
                        .LoadAsync(ct);
                    await Entry(environment)
                        .Collection(e => e.Strategies)
                        .Query()
                        .Include(s => s.Constraints)
                        .Include(s => s.Variants)
                        .LoadAsync(ct);
                }

                configuration = ConfigurationChanges.Capture(previousEnvironment);
                break;
            case FeatureStrategy strategy:
                var previousStrategy = original ? await FeatureStrategies
                    .AsNoTracking()
                    .Include(s => s.Constraints)
                    .Include(s => s.Variants)
                    .SingleAsync(s => s.Id == strategy.Id, ct) : strategy;
                configuration = ConfigurationChanges.Capture(previousStrategy, 0);
                break;
            case Segment segment:
                if (!original && entry.State != EntityState.Added)
                {
                    await Entry(segment)
                        .Collection(s => s.Constraints)
                        .LoadAsync(ct);
                }

                var previousSegment = original ? await Segments
                    .AsNoTracking()
                    .Include(s => s.Constraints)
                    .SingleAsync(s => s.Id == segment.Id, ct) : segment;
                configuration = previousSegment.Constraints
                    .Select(ConfigurationChanges.Capture)
                    .ToArray();
                break;
        }

        if (configuration is not null)
        {
            values[ConfigurationChanges.SnapshotField] = configuration;
        }

        return JsonSerializer.Serialize(values);
    }
}

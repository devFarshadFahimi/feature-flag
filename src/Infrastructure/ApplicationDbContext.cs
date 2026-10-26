using System.Reflection;
using Application.Common.Services;
using BusinessMakerFramework.Infrastructure.SqlCommand;
using Domain.Aggregates.ApiTokenAggregate;
using Domain.Aggregates.ChangeRequestAggregate;
using Domain.Aggregates.FeatureAggregate;
using Domain.Aggregates.ProjectAggregate;
using Domain.Aggregates.SegmentAggregate;
using Domain.Aggregates.UserAggregate;
using Domain.ValueObjects;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using BusinessMakerFramework.Shared.Abstractions.UserManagement;
using Domain.Aggregates.HistoryAggregate;
using System.Text.Json;

namespace Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserInfoService? actor = null) : BaseCommandDbContext(options), IApplicationDbContext, IDataProtectionKeyContext
{
    // Projects
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectEnvironmentPolicy> ProjectEnvironmentPolicies => Set<ProjectEnvironmentPolicy>();
    // Features
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<FeatureEnvironment> FeatureEnvironments => Set<FeatureEnvironment>();
    public DbSet<FeatureStrategy> FeatureStrategies => Set<FeatureStrategy>();
    public DbSet<Constraint> StrategyConstraints => Set<Constraint>();
    public DbSet<Variant> FeatureVariants => Set<Variant>();
    // Environments
    public DbSet<Domain.Aggregates.EnvironmentAggregate.Environment> Environments => Set<Domain.Aggregates.EnvironmentAggregate.Environment>();
    public DbSet<ApiToken> ApiTokens => Set<ApiToken>();
    // Segments
    public DbSet<Segment> Segments => Set<Segment>();
    public DbSet<Constraint> SegmentConstraints => Set<Constraint>(); // separate table
    // Change Requests
    public DbSet<ChangeRequest> ChangeRequests => Set<ChangeRequest>();
    public DbSet<ChangeRequestItem> ChangeRequestItems => Set<ChangeRequestItem>();
    // Users
    public DbSet<User> Users => Set<User>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<ConfigurationHistory> ConfigurationHistory => Set<ConfigurationHistory>();
    public DbSet<Domain.Aggregates.UsageAggregate.FlagUsage> FlagUsage => Set<Domain.Aggregates.UsageAggregate.FlagUsage>();
    public DbSet<Domain.Aggregates.UsageAggregate.ImpressionBatch> ImpressionBatches => Set<Domain.Aggregates.UsageAggregate.ImpressionBatch>();

    public Task SaveChangeAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(cancellationToken);
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        var entries = ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && e.Metadata.FindProperty("Revision") != null)
            .ToArray();

        foreach (var entry in entries)
        {
            var revision = entry.Property("Revision");
            var beforeRevision = (long)(revision.OriginalValue ?? 0L);

            revision.CurrentValue = beforeRevision + 1;
            Dictionary<string, object?> Snapshot(bool original) => entry.Properties
                .Where(p => !p.Metadata.IsShadowProperty() && p.Metadata.Name is not "BusinessId" and not "Version")
                .ToDictionary(p => p.Metadata.Name, p => original ? p.OriginalValue : p.CurrentValue);

            var id = string.Join(":", entry.Metadata.FindPrimaryKey()!.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString()));

            Guid? IdValue(string name) => entry.Metadata.FindProperty(name) is not null && entry.Property(name).CurrentValue is Guid value ? value : null;

            var historyProjectId = entry.Entity is Project project ? project.Id : IdValue("ProjectId");

            if (historyProjectId is null && IdValue("FeatureId") is Guid featureId)
            {
                historyProjectId = Features.Local.FirstOrDefault(f => f.Id == featureId)?.ProjectId ?? await Features
                    .Where(f => f.Id == featureId)
                    .Select(f => (Guid?)f.ProjectId)
                    .SingleOrDefaultAsync(cancellationToken);
            }


            ConfigurationHistory.Add(new ConfigurationHistory { ActorId = Guid.TryParse(actor?.GetClaim("sub"), out var userId) ? userId : null, ProjectId = historyProjectId, EnvironmentId = entry.Entity is Domain.Aggregates.EnvironmentAggregate.Environment environment ? environment.Id : IdValue("EnvironmentId"), ResourceId = id, ResourceType = entry.Metadata.ClrType.Name, Operation = entry.State.ToString(), Revision = beforeRevision + 1, Before = entry.State == EntityState.Added ? null : JsonSerializer.Serialize(Snapshot(true)), After = entry.State == EntityState.Deleted ? null : JsonSerializer.Serialize(Snapshot(false)) });
        }


        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        _ = modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        // Handle Constraint being mapped to two tables via separate entity types
        // Option: use Table-per-Type or split into StrategyConstraint/SegmentConstraint
        _ = modelBuilder
            .Entity<Constraint>()
            .ToTable("StrategyConstraints"); // default; override in specific config
        modelBuilder
            .Entity<Constraint>()
            .ToTable(t => t.HasCheckConstraint("CK_Constraint_OneOwner", "(\"SegmentId\" IS NULL) <> (\"StrategyId\" IS NULL)"));
        modelBuilder
            .Entity<Variant>()
            .ToTable(t => t.HasCheckConstraint("CK_Variant_OneOwner", "(\"FeatureEnvironmentId\" IS NULL) <> (\"StrategyId\" IS NULL)"));
        foreach (var entity in modelBuilder.Model
            .GetEntityTypes()
            .Where(e => e.ClrType.GetProperty("Revision") != null))
        {
            modelBuilder
                .Entity(entity.ClrType)
                .Property<long>("Revision")
                .IsConcurrencyToken();
        }

        modelBuilder
            .Entity<Feature>()
            .Property(f => f.Prerequisites)
            .HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), v => JsonSerializer.Deserialize<List<Domain.Evaluation.Prerequisite>>(v, (JsonSerializerOptions?)null) ?? new())
            .HasColumnType("jsonb").Metadata.SetValueComparer(new Configurations.JsonValueComparer<List<Domain.Evaluation.Prerequisite>>());
        modelBuilder
            .Entity<FeatureStrategy>()
            .Property(s => s.Enabled)
            .HasDefaultValue(true);

        var history = modelBuilder.Entity<ConfigurationHistory>();

        history.ToTable("ConfigurationHistory", "App");
        history.HasKey(h => h.Id);
        history
            .Property(h => h.ResourceType)
            .HasMaxLength(128);
        history
            .Property(h => h.ResourceId)
            .HasMaxLength(128);
        history
            .Property(h => h.Before)
            .HasColumnType("jsonb");
        history
            .Property(h => h.After)
            .HasColumnType("jsonb");
        history.HasIndex(h => new { h.ProjectId, h.Timestamp });

        var usage = modelBuilder.Entity<Domain.Aggregates.UsageAggregate.FlagUsage>();

        usage.ToTable("FlagUsage", "App");
        usage.HasKey(u => new { u.ProjectId, u.EnvironmentId, u.AppName, u.FeatureName, u.Variant, u.Enabled, u.Impression, u.Hour });
        usage
            .Property(u => u.AppName)
            .HasMaxLength(128);
        usage
            .Property(u => u.FeatureName)
            .HasMaxLength(255);
        usage
            .Property(u => u.Variant)
            .HasMaxLength(128);
        usage.HasIndex(u => u.Hour);

        var batches = modelBuilder.Entity<Domain.Aggregates.UsageAggregate.ImpressionBatch>();

        batches.ToTable("ImpressionBatch", "App");
        batches.HasKey(b => new { b.ProjectId, b.EnvironmentId, b.BatchId });
        batches.HasIndex(b => b.ReceivedAt);

        var policy = modelBuilder.Entity<ProjectEnvironmentPolicy>();

        policy.ToTable("ProjectEnvironmentPolicy", "App");
        policy.HasKey(p => new { p.ProjectId, p.EnvironmentId });
        policy
            .Property(p => p.Revision)
            .IsConcurrencyToken();
        policy
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        policy
            .HasOne<Domain.Aggregates.EnvironmentAggregate.Environment>()
            .WithMany()
            .HasForeignKey(p => p.EnvironmentId)
            .OnDelete(DeleteBehavior.Cascade);

        var requests = modelBuilder.Entity<ChangeRequest>();

        requests.Ignore(r => r.Scope);
        requests
            .Property(r => r.ScopeFingerprint)
            .HasMaxLength(128);
        requests
            .Property(r => r.FailureReason)
            .HasMaxLength(2000);
        requests
            .Property(r => r.RejectionReason)
            .HasMaxLength(2000);
        requests
            .Property(r => r.CapturedRevisions)
            .HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), v => JsonSerializer.Deserialize<Dictionary<string, long>>(v, (JsonSerializerOptions?)null) ?? new())
            .HasColumnType("jsonb");
        requests
            .Property(r => r.ResultRevisions)
            .HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), v => JsonSerializer.Deserialize<Dictionary<string, long>>(v, (JsonSerializerOptions?)null) ?? new())
            .HasColumnType("jsonb");
        foreach (var property in modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.GetValueConverter() != null && p.ClrType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.ClrType)))
        {
            property.SetValueComparer((Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer)Activator.CreateInstance(typeof(Configurations.JsonValueComparer<>).MakeGenericType(property.ClrType))!);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        _ = configurationBuilder
            .Properties<string>()
            .HaveMaxLength(50);
    }
}
using Domain.Aggregates.SegmentAggregate;
using Domain.ValueObjects;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace Application.Common.Services;

public interface IApplicationDbContext
{
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<Feature> Features { get; }
    DbSet<FeatureEnvironment> FeatureEnvironments { get; }
    DbSet<FeatureStrategy> FeatureStrategies { get; }
    DbSet<Constraint> StrategyConstraints { get; }
    DbSet<Variant> FeatureVariants { get; }
    DbSet<Domain.Aggregates.EnvironmentAggregate.Environment> Environments { get; }
    DbSet<ApiToken> ApiTokens { get; }
    DbSet<Segment> Segments { get; }
    DbSet<Constraint> SegmentConstraints { get; }
    DbSet<ChangeRequest> ChangeRequests { get; }
    DbSet<ChangeRequestItem> ChangeRequestItems { get; }
    DbSet<User> Users { get; }

    DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    Task SaveChangeAsync(CancellationToken cancellationToken = default);
}

using Domain.Aggregates.SegmentAggregate;
using Domain.ValueObjects;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace Application.Common.Services;

public interface IApplicationDbContext
{
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database
    {
        get;
    }

    DbSet<Project> Projects
    {
        get;
    }

    DbSet<ProjectMember> ProjectMembers
    {
        get;
    }

    DbSet<ProjectEnvironmentPolicy> ProjectEnvironmentPolicies
    {
        get;
    }

    DbSet<Feature> Features
    {
        get;
    }

    DbSet<FeatureEnvironment> FeatureEnvironments
    {
        get;
    }

    DbSet<FeatureStrategy> FeatureStrategies
    {
        get;
    }

    DbSet<Constraint> StrategyConstraints
    {
        get;
    }

    DbSet<Variant> FeatureVariants
    {
        get;
    }

    DbSet<Domain.Aggregates.EnvironmentAggregate.Environment> Environments
    {
        get;
    }

    DbSet<Segment> Segments
    {
        get;
    }

    DbSet<Constraint> SegmentConstraints
    {
        get;
    }

    DbSet<ChangeRequest> ChangeRequests
    {
        get;
    }

    DbSet<ChangeRequestItem> ChangeRequestItems
    {
        get;
    }

    DbSet<User> Users
    {
        get;
    }

    DbSet<Domain.Aggregates.HistoryAggregate.ConfigurationHistory> ConfigurationHistory
    {
        get;
    }

    DbSet<Domain.Aggregates.UsageAggregate.FlagUsage> FlagUsage
    {
        get;
    }

    DbSet<Domain.Aggregates.UsageAggregate.ImpressionBatch> ImpressionBatches
    {
        get;
    }

    DbSet<DataProtectionKey> DataProtectionKeys
    {
        get; set;
    }

    Task SaveChangeAsync(CancellationToken cancellationToken = default);
}

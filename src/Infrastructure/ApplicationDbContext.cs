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

namespace Infrastructure;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : BaseCommandDbContext(options), IApplicationDbContext, IDataProtectionKeyContext
{
    // Projects
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

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
    public Task SaveChangeAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(cancellationToken);

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        _ = optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Handle Constraint being mapped to two tables via separate entity types
        // Option: use Table-per-Type or split into StrategyConstraint/SegmentConstraint
        _ = modelBuilder.Entity<Constraint>()
            .ToTable("StrategyConstraints"); // default; override in specific config

    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        _ = configurationBuilder.Properties<string>().HaveMaxLength(50);
    }
}

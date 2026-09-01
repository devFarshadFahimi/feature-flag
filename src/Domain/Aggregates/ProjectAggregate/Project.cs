using Domain.Aggregates.FeatureAggregate;
using Domain.Aggregates.FeatureAggregate.Events;
using Domain.Aggregates.ProjectAggregate.Events;

namespace Domain.Aggregates.ProjectAggregate;

public sealed class Project : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string DefaultStickiness { get; private set; }
    public bool FeatureLimitEnabled { get; private set; }
    public int? FeatureLimit { get; private set; }

    private readonly List<Feature> _features = [];
    public IReadOnlyCollection<Feature> Features => _features.AsReadOnly();

    private readonly List<ProjectMember> _members = [];
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private Project(Guid id, string name, string description, string defaultStickiness)
    {
        Id = id;
        Name = name;
        Description = description;
        DefaultStickiness = defaultStickiness;
    }

    public static Project Create(string name, string description, string defaultStickiness = "default")
    {
        var project = new Project(Guid.NewGuid(), name, description, defaultStickiness);
        project.Apply(new ProjectCreatedEvent(project.Id, name));
        return project;
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        Apply(new ProjectUpdatedEvent(Id, name));
    }

    public Feature AddFeature(string featureName, FeatureType type, string? description = null)
    {
        if (FeatureLimitEnabled && FeatureLimit.HasValue && _features.Count >= FeatureLimit.Value)
        {
            throw new InvalidEntityStateException($"Project has reached feature limit of {FeatureLimit}");
        }

        if (_features.Any(f => f.Name == featureName))
        {
            throw new InvalidEntityStateException($"Feature '{featureName}' already exists in project");
        }

        var feature = Feature.Create(Id, featureName, type, description);
        _features.Add(feature);
        return feature;
    }

    public void RemoveFeature(Guid featureId)
    {
        var feature = _features.FirstOrDefault(f => f.Id == featureId)
            ?? throw new InvalidEntityStateException($"Feature {featureId} not found");

        feature.Archive();
        Apply(new FeatureArchivedEvent(featureId, Id));
    }

    public void AddMember(Guid userId, ProjectRole role)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            throw new InvalidEntityStateException($"User {userId} is already a member");
        }

        _members.Add(new ProjectMember(userId, role));
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidEntityStateException($"User {userId} is not a member");

        _ = _members.Remove(member);
    }

    public void SetFeatureLimit(int? limit)
    {
        FeatureLimitEnabled = limit.HasValue;
        FeatureLimit = limit;
    }
}

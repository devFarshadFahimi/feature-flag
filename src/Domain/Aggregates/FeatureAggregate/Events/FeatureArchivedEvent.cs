using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.FeatureAggregate.Events;

public record FeatureArchivedEvent(Guid FeatureId, Guid ProjectId) : DomainEvent(nameof(FeatureArchivedEvent));

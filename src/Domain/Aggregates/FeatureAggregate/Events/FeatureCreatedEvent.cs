using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.FeatureAggregate.Events;

public record FeatureCreatedEvent(Guid FeatureId, Guid ProjectId, string Name) : DomainEvent(nameof(FeatureCreatedEvent));

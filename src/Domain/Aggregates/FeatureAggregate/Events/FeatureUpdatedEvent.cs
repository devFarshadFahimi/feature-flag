using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.FeatureAggregate.Events;

public record FeatureUpdatedEvent(Guid FeatureId, Guid ProjectId, string Name) : DomainEvent(nameof(FeatureUpdatedEvent));

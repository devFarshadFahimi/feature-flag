using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.FeatureAggregate.Events;

public record FeatureEnabledEvent(Guid FeatureId, Guid EnvironmentId) : DomainEvent(nameof(FeatureEnabledEvent));

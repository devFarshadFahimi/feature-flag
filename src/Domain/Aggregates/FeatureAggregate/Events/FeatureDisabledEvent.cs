using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.FeatureAggregate.Events;

public record FeatureDisabledEvent(Guid FeatureId, Guid EnvironmentId) : DomainEvent(nameof(FeatureDisabledEvent));
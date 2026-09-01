using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.ChangeRequestAggregate.Events;

public record ChangeRequestCreatedEvent(Guid ChangeRequestId, Guid ProjectId, Guid EnvironmentId) : DomainEvent(nameof(ChangeRequestCreatedEvent));

using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.ChangeRequestAggregate.Events;

public record ChangeRequestApprovedEvent(Guid ChangeRequestId, Guid ReviewerId) : DomainEvent(nameof(ChangeRequestApprovedEvent));

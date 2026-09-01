using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.ChangeRequestAggregate.Events;

public record ChangeRequestAppliedEvent(Guid ChangeRequestId) : DomainEvent(nameof(ChangeRequestAppliedEvent));
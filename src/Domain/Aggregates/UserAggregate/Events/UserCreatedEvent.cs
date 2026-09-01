using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.UserAggregate.Events;

public record UserCreatedEvent(Guid UserId, string Email) : DomainEvent(nameof(UserCreatedEvent));

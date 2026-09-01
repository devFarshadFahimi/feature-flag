using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.EnvironmentAggregate.Events;

public record EnvironmentCreatedEvent(Guid EnvironmentId, string Name, EnvironmentType EnvType) : DomainEvent(nameof(EnvironmentCreatedEvent));

using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.ProjectAggregate.Events;

public record ProjectCreatedEvent(Guid ProjectId, string Name) : DomainEvent(nameof(ProjectCreatedEvent));

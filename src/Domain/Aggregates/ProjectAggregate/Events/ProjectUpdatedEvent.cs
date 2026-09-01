using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.ProjectAggregate.Events;

public record ProjectUpdatedEvent(Guid ProjectId, string Name) : DomainEvent(nameof(ProjectUpdatedEvent));
using BusinessMakerFramework.Domain.Core.Events;

namespace Domain.Aggregates.SegmentAggregate.Events;

public record SegmentCreatedEvent(int SegmentId, string Name) : DomainEvent(nameof(SegmentCreatedEvent));

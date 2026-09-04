using Application.Features.Segments.Queries.GetSegmentById;
using Domain.Aggregates.SegmentAggregate;
using Domain.ValueObjects;

namespace Application.Common.Mappers;

public static class SegmentMappingConfig
{
    public static void Configure()
    {
        // Constraint → SegmentConstraintResponse
        _ = TypeAdapterConfig<Constraint, SegmentConstraintResponse>
            .NewConfig()
            .Map(dest => dest.Operator, src => src.Operator.ToString())
            .Map(dest => dest.Values, src => src.Values.ToList());

        // Segment → SegmentResponse
        _ = TypeAdapterConfig<Segment, SegmentResponse>
            .NewConfig()
            .Map(dest => dest.Constraints, src => src.Constraints.Adapt<List<SegmentConstraintResponse>>());
    }
}

using Domain.Aggregates.SegmentAggregate;

namespace Application.Features.Segments.Commands.RemoveSegmentConstraint;

public record RemoveSegmentConstraintCommand(int SegmentId, int ConstraintIndex) : ICommandRequest;

internal class RemoveSegmentConstraintCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<RemoveSegmentConstraintCommand>
{
    public override async Task<Result> Handle(RemoveSegmentConstraintCommand request, CancellationToken cancellationToken)
    {
        var segment = await dbContext.Segments.FindAsync([request.SegmentId], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Segment), request.SegmentId + string.Empty);

        segment.RemoveConstraint(request.ConstraintIndex);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}
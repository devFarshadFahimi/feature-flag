using Domain.Aggregates.SegmentAggregate;

namespace Application.Features.Segments.Commands.DeleteSegment;

public record DeleteSegmentCommand(int Id) : ICommandRequest;

internal class DeleteSegmentCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<DeleteSegmentCommand>
{
    public override async Task<Result> Handle(DeleteSegmentCommand request, CancellationToken cancellationToken)
    {
        var segment = await dbContext.Segments.FindAsync([request.Id], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Segment), request.Id + string.Empty);

        _ = dbContext.Segments.Remove(segment);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Segments;
using Domain.Exceptions;

namespace Application.Features.Segments.Commands.DeleteSegment;

public record DeleteSegmentCommand(int Id) : ICommandRequest;

internal class DeleteSegmentCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<DeleteSegmentCommand>
{
    public override async Task<Result> Handle(DeleteSegmentCommand request, CancellationToken cancellationToken)
{
    var segment = await dbContext.Segments.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Segment), request.Id);

    dbContext.Segments.Remove(segment);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}
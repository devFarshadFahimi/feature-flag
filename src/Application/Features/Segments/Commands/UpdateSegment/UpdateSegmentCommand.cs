using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Segments;
using Domain.Exceptions;

namespace Application.Features.Segments.Commands.UpdateSegment;

public record UpdateSegmentCommand(int Id, string Name, string? Description) : ICommandRequest;

internal class UpdateSegmentCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<UpdateSegmentCommand>
{
    public override async Task<Result> Handle(UpdateSegmentCommand request, CancellationToken cancellationToken)
{
    var segment = await dbContext.Segments.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Segment), request.Id);

    segment.Update(request.Name, request.Description);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}
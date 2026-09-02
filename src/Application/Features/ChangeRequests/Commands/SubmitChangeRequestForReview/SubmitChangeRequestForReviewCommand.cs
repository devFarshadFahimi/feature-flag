using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.ChangeRequests;
using Domain.Exceptions;
using MediatR;

namespace Application.Features.ChangeRequests.Commands.SubmitChangeRequestForReview;

public record SubmitChangeRequestForReviewCommand(Guid Id) : ICommandRequest;

internal class SubmitChangeRequestForReviewCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<SubmitChangeRequestForReviewCommand>
{
    public override async Task<Result> Handle(SubmitChangeRequestForReviewCommand request, CancellationToken cancellationToken)
{
    var changeRequest = await dbContext.ChangeRequests.FindAsync([request.Id], cancellationToken)
            ?? throw new EntityNotFoundException(nameof(ChangeRequest), request.Id);

    changeRequest.SubmitForReview();
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok();
}
}
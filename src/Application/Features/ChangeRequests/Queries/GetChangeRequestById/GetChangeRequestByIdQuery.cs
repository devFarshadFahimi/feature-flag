using Application.Common.Interfaces;
using Domain.Aggregates.ChangeRequests;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChangeRequests.Queries.GetChangeRequestById;

public record GetChangeRequestByIdQuery(Guid Id) : IQueryRequest<ChangeRequestResponse>;

public record ChangeRequestResponse(
    Guid Id,
    Guid ProjectId,
    Guid EnvironmentId,
    Guid CreatedBy,
    string Status,
    string? Title,
    string? Description,
    DateTime? ScheduledAt,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedBy,
    List<ChangeRequestItemResponse> Items);

public record ChangeRequestItemResponse(
    Guid Id,
    string Action,
    Guid? FeatureId,
    string? Payload);

internal class GetChangeRequestByIdQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetChangeRequestByIdQuery, ChangeRequestResponse>
{
    public override async Task<ChangeRequestResponse> Handle(GetChangeRequestByIdQuery request, CancellationToken cancellationToken)
{
    var changeRequest = await dbContext.ChangeRequests
        .Include(cr => cr.Items)
        .FirstOrDefaultAsync(cr => cr.Id == request.Id, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(ChangeRequest), request.Id);

    return new ChangeRequestResponse(
        changeRequest.Id,
        changeRequest.ProjectId,
        changeRequest.EnvironmentId,
        changeRequest.CreatedBy,
        changeRequest.Status.ToString(),
        changeRequest.Title,
        changeRequest.Description,
        changeRequest.ScheduledAt,
        changeRequest.CreatedAt,
        changeRequest.ReviewedAt,
        changeRequest.ReviewedBy,
        changeRequest.Items.Select(i => new ChangeRequestItemResponse(
            i.Id,
            i.Action,
            i.FeatureId,
            i.Payload)).ToList());
}
}
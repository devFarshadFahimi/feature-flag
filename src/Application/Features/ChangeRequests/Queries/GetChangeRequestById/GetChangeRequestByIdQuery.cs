namespace Application.Features.ChangeRequests.Queries.GetChangeRequestById;

public record GetChangeRequestByIdQuery(Guid Id) : IQueryRequest<ChangeRequestResponse>;
public record ChangeRequestResponse(Guid Id, Guid? ProjectId, Guid EnvironmentId, Guid CreatedBy, string Status, string? Title, string? Description, DateTime? ScheduledAt, DateTime CreatedAt, DateTime? ReviewedAt, Guid? ReviewedBy, List<ChangeRequestItemResponse> Items, long Revision = 0, string Scope = "Project", int RequiredApprovals = 1, List<Guid>? Reviewers = null, string? RejectionReason = null, string? FailureReason = null, int Attempts = 0, DateTime? NextAttemptAt = null, Guid? PredecessorId = null, Guid? ScheduledBy = null, Dictionary<string, long>? CapturedRevisions = null, Dictionary<string, long>? ResultRevisions = null);
public record ChangeRequestItemResponse(Guid Id, string Action, Guid? FeatureId, string? Payload);
internal class GetChangeRequestByIdQueryHandler(IApplicationDbContext dbContext) : QueryRequestHandler<GetChangeRequestByIdQuery, ChangeRequestResponse>
{
    public override async Task<ChangeRequestResponse> Handle(GetChangeRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests
            .Include(cr => cr.Items)
            .Where(cr => cr.Id == request.Id)
            .ProjectToType<ChangeRequestResponse>()
            .FirstOrDefaultAsync(cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        return changeRequest;
    }
}
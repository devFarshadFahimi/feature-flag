using Application.Features.FeatureEnvironments;

namespace Application.Features.ChangeRequests;

public record SetApprovalPolicyCommand(Guid ProjectId, Guid EnvironmentId, long ExpectedRevision, bool Enabled, int RequiredApprovals = 1) : ICommandRequest;
public record ProtectEnvironmentCommand(Guid EnvironmentId, long ExpectedRevision) : ICommandRequest;
internal sealed class SetApprovalPolicyHandler(IApplicationDbContext db) : CommandRequestHandler<SetApprovalPolicyCommand>
{
    public override async Task<Result> Handle(SetApprovalPolicyCommand request, CancellationToken ct)
    {
        if (request.RequiredApprovals is < 1 or > 5)
        {
            throw new ArgumentException("تعداد تأیید لازم باید بین ۱ تا ۵ باشد.");
        }

        if (!await db.Environments.AnyAsync(e => e.Id == request.EnvironmentId, ct))
        {
            throw new KeyNotFoundException("محیط یافت نشد.");
        }

        var policy = await db.ProjectEnvironmentPolicies.SingleOrDefaultAsync(p => p.ProjectId == request.ProjectId && p.EnvironmentId == request.EnvironmentId, ct);

        FeatureConfiguration.RequireRevision(request.ExpectedRevision, policy?.Revision ?? 0);

        if (policy?.Enabled == true && (!request.Enabled || request.RequiredApprovals < policy.RequiredApprovals))
        {
            throw new InvalidEntityStateException("کاهش حفاظت به درخواست تغییر تأییدشده نیاز دارد.");
        }

        if (policy is null)
        {
            policy = new ProjectEnvironmentPolicy
            {
                ProjectId = request.ProjectId,
                EnvironmentId = request.EnvironmentId
            };
            db.ProjectEnvironmentPolicies.Add(policy);
        }

        policy.Enabled = request.Enabled;
        policy.RequiredApprovals = request.RequiredApprovals;

        await db.SaveChangeAsync(ct);

        return Ok();
    }
}

internal sealed class ProtectEnvironmentHandler(IApplicationDbContext db) : CommandRequestHandler<ProtectEnvironmentCommand>
{
    public override async Task<Result> Handle(ProtectEnvironmentCommand request, CancellationToken ct)
    {
        var environment = await db.Environments.SingleOrDefaultAsync(e => e.Id == request.EnvironmentId, ct) ?? throw new KeyNotFoundException("محیط یافت نشد.");

        FeatureConfiguration.RequireRevision(request.ExpectedRevision, environment.Revision);
        environment.SetProtected(true);

        await db.SaveChangeAsync(ct);

        return Ok();
    }
}
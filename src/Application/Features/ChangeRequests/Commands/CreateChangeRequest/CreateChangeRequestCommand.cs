namespace Application.Features.ChangeRequests.Commands.CreateChangeRequest;

public record CreateChangeRequestCommand(Guid? ProjectId, Guid EnvironmentId, Guid CreatedBy, string? Title = null, string? Description = null, Guid? PredecessorId = null) : ICommandRequest<Guid>;
internal class CreateChangeRequestCommandHandler(IApplicationDbContext dbContext, BusinessMakerFramework.Shared.Abstractions.UserManagement.IUserInfoService actor) : CommandRequestHandler<CreateChangeRequestCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(CreateChangeRequestCommand request, CancellationToken cancellationToken)
    {
        if (!request.ProjectId.HasValue)
        {
            await ConfigurationChanges.EnsureOwnerAsync(dbContext, null, actor.Id(), false, cancellationToken);
        }

        var changeRequest = request.ProjectId.HasValue ? ChangeRequest.Create(request.ProjectId.Value, request.EnvironmentId, actor.Id(), request.Title, request.Description) : ChangeRequest.CreateGlobal(request.EnvironmentId, actor.Id(), request.Title, request.Description);
        var previousId = request.PredecessorId;
        var chain = new HashSet<Guid>
        {
            changeRequest.Id
        };

        while (previousId.HasValue)
        {
            if (chain.Count >= 20 || !chain.Add(previousId.Value))
            {
                throw new ArgumentException("زنجیره مراحل دارای چرخه یا عمق غیرمجاز است.");
            }

            var previous = await dbContext.ChangeRequests.SingleOrDefaultAsync(r => r.Id == previousId, cancellationToken) ?? throw new KeyNotFoundException("مرحله پیشین یافت نشد.");

            if (previous.ProjectId != request.ProjectId || previous.EnvironmentId != request.EnvironmentId)
            {
                throw new ArgumentException("مرحله پیشین باید متعلق به همین پروژه و محیط باشد.");
            }

            previousId = previous.PredecessorId;
        }

        changeRequest.SetPredecessor(request.PredecessorId);
        _ = dbContext.ChangeRequests.Add(changeRequest);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok(changeRequest.Id);
    }
}
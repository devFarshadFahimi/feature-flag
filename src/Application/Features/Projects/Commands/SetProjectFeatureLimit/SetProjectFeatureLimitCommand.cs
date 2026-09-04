namespace Application.Features.Projects.Commands.SetProjectFeatureLimit;

public record SetProjectFeatureLimitCommand(Guid ProjectId, int? Limit) : ICommandRequest;

internal class SetProjectFeatureLimitCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<SetProjectFeatureLimitCommand>
{
    public override async Task<Result> Handle(SetProjectFeatureLimitCommand request, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects.FindAsync([request.ProjectId], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Project), request.ProjectId + string.Empty);

        project.SetFeatureLimit(request.Limit);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}
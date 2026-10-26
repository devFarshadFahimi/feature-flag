namespace Application.Features.ChangeRequests.Commands.AddChangeRequestItem;

public record AddChangeRequestItemCommand(Guid ChangeRequestId, string Action, Guid? FeatureId = null, string? Payload = null) : ICommandRequest<Guid>;
internal class AddChangeRequestItemCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<AddChangeRequestItemCommand, Guid>
{
    public override async Task<Result<Guid>> Handle(AddChangeRequestItemCommand request, CancellationToken cancellationToken)
    {
        var changeRequest = await dbContext.ChangeRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.ChangeRequestId, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");
        var item = ChangeRequestItem.Create(request.Action, request.FeatureId, request.Payload);

        _ = ConfigurationChanges.Parse(item);
        changeRequest.AddItem(item);
        dbContext.ChangeRequestItems.Add(item);
        await ConfigurationChanges.SuspendSuccessorsAsync(dbContext, changeRequest.Id, cancellationToken);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok(item.Id);
    }
}
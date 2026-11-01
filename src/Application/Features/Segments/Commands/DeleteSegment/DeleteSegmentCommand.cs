using Domain.Aggregates.SegmentAggregate;

namespace Application.Features.Segments.Commands.DeleteSegment;

public record DeleteSegmentCommand(int Id) : ICommandRequest;
internal class DeleteSegmentCommandHandler(IApplicationDbContext dbContext) : CommandRequestHandler<DeleteSegmentCommand>
{
    public override async Task<Result> Handle(DeleteSegmentCommand request, CancellationToken cancellationToken)
    {
        var segment = await dbContext.Segments.FindAsync([request.Id], cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        if (await Application.Features.ChangeRequests.ConfigurationChanges
            .SegmentReferences(dbContext, segment.Id)
            .AnyAsync(cancellationToken))
        {
            throw new BusinessMakerFramework.Domain.Core.Exceptions.InvalidEntityStateException("بخش در راهبردها استفاده می‌شود؛ ابتدا ارجاع‌های آن را حذف کنید.");
        }

        _ = dbContext.Segments.Remove(segment);

        await dbContext.SaveChangeAsync(cancellationToken);

        return Ok();
    }
}
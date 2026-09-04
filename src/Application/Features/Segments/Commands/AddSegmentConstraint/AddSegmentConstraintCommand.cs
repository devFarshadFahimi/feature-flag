using Domain.Aggregates.SegmentAggregate;
using Domain.ValueObjects;

namespace Application.Features.Segments.Commands.AddSegmentConstraint;

public record AddSegmentConstraintCommand(
    int SegmentId,
    string ContextName,
    ConstraintOperator Operator,
    List<string> Values,
    bool Inverted = false,
    bool CaseInsensitive = false) : ICommandRequest;

internal class AddSegmentConstraintCommandHandler(IApplicationDbContext dbContext)
    : CommandRequestHandler<AddSegmentConstraintCommand>
{
    public override async Task<Result> Handle(AddSegmentConstraintCommand request, CancellationToken cancellationToken)
    {
        var segment = await dbContext.Segments.FindAsync([request.SegmentId], cancellationToken)
                ?? throw new InvalidEntityStateException(nameof(Segment), request.SegmentId + string.Empty);

        var constraint = Constraint.Create(request.ContextName, request.Operator, request.Values, request.Inverted, request.CaseInsensitive);
        segment.AddConstraint(constraint);
        await dbContext.SaveChangeAsync(cancellationToken);
        return Ok();
    }
}
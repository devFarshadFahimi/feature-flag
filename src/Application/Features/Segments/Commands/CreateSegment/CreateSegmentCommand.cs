using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Aggregates.Segments;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Features.Segments.Commands.CreateSegment;

public record CreateSegmentCommand(
    string Name,
    string? Description = null,
    bool IsPublic = true,
    List<ConstraintDto>? Constraints = null) : ICommandRequest<int>;

public record ConstraintDto(
    string ContextName,
    ConstraintOperator Operator,
    List<string> Values,
    bool Inverted = false,
    bool CaseInsensitive = false);

internal class CreateSegmentCommandHandler(IApplicationDbContext dbContext) 
    : CommandRequestHandler<CreateSegmentCommand, int>
{
    public override async Task<Result<int>> Handle(CreateSegmentCommand request, CancellationToken cancellationToken)
{
    var constraints = request.Constraints?.Select(c =>
        Constraint.Create(c.ContextName, c.Operator, c.Values, c.Inverted, c.CaseInsensitive)).ToList();

    var segment = Segment.Create(request.Name, request.Description, constraints, request.IsPublic);
    dbContext.Segments.Add(segment);
    await dbContext.SaveChangeAsync(cancellationToken);
    return Ok(segment.Id);
}
}
using Application.Common.DTOs;

namespace Application.Features.Environments.Queries.GetEnvironmentsSelective;

public record GetEnvironmentsSelectiveQuery : IQueryRequest<List<SelectiveDTO<Guid>>>;

internal class GetEnvironmentsSelectiveQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetEnvironmentsSelectiveQuery, List<SelectiveDTO<Guid>>>
{
    public override async Task<List<SelectiveDTO<Guid>>> Handle(GetEnvironmentsSelectiveQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Environments
            .AsNoTracking()
            .OrderBy(environment => environment.SortOrder)
            .ThenBy(environment => environment.Name)
            .ThenBy(environment => environment.Id)
            .Select(environment => new SelectiveDTO<Guid>(environment.Id, environment.Name))
            .ToListAsync(cancellationToken);
    }
}

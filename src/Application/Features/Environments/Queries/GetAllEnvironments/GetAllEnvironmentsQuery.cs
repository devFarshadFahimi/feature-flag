namespace Application.Features.Environments.Queries.GetAllEnvironments;

public record GetAllEnvironmentsQuery : IQueryRequest<List<EnvironmentResponse>>;

internal class GetAllEnvironmentsQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetAllEnvironmentsQuery, List<EnvironmentResponse>>
{
    public override async Task<List<EnvironmentResponse>> Handle(GetAllEnvironmentsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Environments
            .Include(e => e.Tokens)
            .ProjectToType<EnvironmentResponse>()
            .ToListAsync(cancellationToken);
    }
}
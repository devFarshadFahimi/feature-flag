namespace Application.Features.Environments.Queries.GetEnvironmentById;

public record GetEnvironmentByIdQuery(Guid Id) : IQueryRequest<EnvironmentResponse>;
public record EnvironmentResponse(Guid Id, string Name, string Type, bool Enabled, int SortOrder, bool Protected, long Revision = 0);
internal class GetEnvironmentByIdQueryHandler(IApplicationDbContext dbContext) : QueryRequestHandler<GetEnvironmentByIdQuery, EnvironmentResponse>
{
    public override async Task<EnvironmentResponse> Handle(GetEnvironmentByIdQuery request, CancellationToken cancellationToken)
    {
        var environment = await dbContext.Environments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        return new EnvironmentResponse(environment.Id, environment.Name, environment.Type.ToString(), environment.Enabled, environment.SortOrder, environment.Protected, environment.Revision);
    }
}

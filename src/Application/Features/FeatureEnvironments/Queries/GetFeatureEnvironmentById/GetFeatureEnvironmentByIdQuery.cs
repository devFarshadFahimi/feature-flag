using Application.Features.Features.Queries.GetFeatureById;

namespace Application.Features.FeatureEnvironments.Queries.GetFeatureEnvironmentById;

public record GetFeatureEnvironmentByIdQuery(Guid FeatureId, Guid EnvironmentId) : IQueryRequest<FeatureEnvironmentResponse>;
internal class GetFeatureEnvironmentByIdQueryHandler(IApplicationDbContext dbContext) : QueryRequestHandler<GetFeatureEnvironmentByIdQuery, FeatureEnvironmentResponse>
{
    public override async Task<FeatureEnvironmentResponse> Handle(GetFeatureEnvironmentByIdQuery request, CancellationToken cancellationToken)
    {
        var featureEnv = await dbContext.FeatureEnvironments
            .Include(fe => fe.Variants)
            .Include(fe => fe.Strategies)
            .ThenInclude(s => s.Variants)
            .Include(fe => fe.Strategies)
            .ThenInclude(s => s.Constraints)
            .FirstOrDefaultAsync(fe => fe.FeatureId == request.FeatureId && fe.EnvironmentId == request.EnvironmentId, cancellationToken) ?? throw new KeyNotFoundException("رکورد درخواستی یافت نشد.");

        return featureEnv.Adapt<FeatureEnvironmentResponse>();
    }
}
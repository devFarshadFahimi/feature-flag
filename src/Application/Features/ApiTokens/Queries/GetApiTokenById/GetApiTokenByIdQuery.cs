namespace Application.Features.ApiTokens.Queries.GetApiTokenById;

public record GetApiTokenByIdQuery(Guid Id) : IQueryRequest<ApiTokenResponse>;

internal class GetApiTokenByIdQueryHandler(IApplicationDbContext dbContext)
    : QueryRequestHandler<GetApiTokenByIdQuery, ApiTokenResponse>
{
    public override async Task<ApiTokenResponse> Handle(GetApiTokenByIdQuery request, CancellationToken cancellationToken)
    {
        var token = await dbContext.ApiTokens
            .Where(t => t.Id == request.Id)
            .ProjectToType<ApiTokenResponse>()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidEntityStateException(nameof(ApiToken), request.Id.ToString());

        return token;
    }
}
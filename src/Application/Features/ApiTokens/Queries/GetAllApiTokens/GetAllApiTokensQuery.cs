using Application.Common.Interfaces;
using Application.Features.Environments.Queries.GetEnvironmentById;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ApiTokens.Queries.GetAllApiTokens;

public record GetAllApiTokensQuery(Guid? EnvironmentId = null) : IQueryRequest<List<ApiTokenResponse>>;

internal class GetAllApiTokensQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetAllApiTokensQuery, List<ApiTokenResponse>>
{
    public override async Task<List<ApiTokenResponse>> Handle(GetAllApiTokensQuery request, CancellationToken cancellationToken)
{
    var query = dbContext.ApiTokens.AsQueryable();

    if (request.EnvironmentId.HasValue)
        query = query.Where(t => t.EnvironmentId == request.EnvironmentId.Value);

    return await query
        .Select(t => new ApiTokenResponse(
            t.Id,
            t.TokenType.ToString(),
            t.Name,
            t.CreatedAt,
            t.ExpiresAt,
            t.LastUsedAt,
            t.IsRevoked))
        .ToListAsync(cancellationToken);
}
}
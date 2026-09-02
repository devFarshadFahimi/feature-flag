using Application.Common.Interfaces;
using Application.Features.Environments.Queries.GetEnvironmentById;
using Domain.Aggregates.ApiTokens;
using Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ApiTokens.Queries.GetApiTokenById;

public record GetApiTokenByIdQuery(Guid Id) : IQueryRequest<ApiTokenResponse>;

internal class GetApiTokenByIdQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetApiTokenByIdQuery, ApiTokenResponse>
{
    public override async Task<ApiTokenResponse> Handle(GetApiTokenByIdQuery request, CancellationToken cancellationToken)
{
    var token = await dbContext.ApiTokens
        .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(ApiToken), request.Id);

    return new ApiTokenResponse(
        token.Id,
        token.TokenType.ToString(),
        token.Name,
        token.CreatedAt,
        token.ExpiresAt,
        token.LastUsedAt,
        token.IsRevoked);
}
}
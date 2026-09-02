using Application.Common.Interfaces;
using Application.Features.Environments.Queries.GetEnvironmentById;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Environments.Queries.GetAllEnvironments;

public record GetAllEnvironmentsQuery : IQueryRequest<List<EnvironmentResponse>>;

internal class GetAllEnvironmentsQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetAllEnvironmentsQuery, List<EnvironmentResponse>>
{
    public override async Task<List<EnvironmentResponse>> Handle(GetAllEnvironmentsQuery request, CancellationToken cancellationToken)
{
    return await dbContext.Environments
        .Include(e => e.Tokens)
        .Select(e => new EnvironmentResponse(
            e.Id,
            e.Name,
            e.Type.ToString(),
            e.Enabled,
            e.SortOrder,
            e.Protected,
            e.Tokens.Select(t => new ApiTokenResponse(
                t.Id,
                t.TokenType.ToString(),
                t.Name,
                t.CreatedAt,
                t.ExpiresAt,
                t.LastUsedAt,
                t.IsRevoked)).ToList()))
        .ToListAsync(cancellationToken);
}
}
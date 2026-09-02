using Application.Common.Interfaces;
using Domain.Aggregates.Environments;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Environments.Queries.GetEnvironmentById;

public record GetEnvironmentByIdQuery(Guid Id) : IQueryRequest<EnvironmentResponse>;

public record EnvironmentResponse(
    Guid Id,
    string Name,
    string Type,
    bool Enabled,
    int SortOrder,
    bool Protected,
    List<ApiTokenResponse> Tokens);

public record ApiTokenResponse(
    Guid Id,
    string TokenType,
    string? Name,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    bool IsRevoked);

internal class GetEnvironmentByIdQueryHandler(IApplicationDbContext dbContext) 
    : QueryRequestHandler<GetEnvironmentByIdQuery, EnvironmentResponse>
{
    public override async Task<EnvironmentResponse> Handle(GetEnvironmentByIdQuery request, CancellationToken cancellationToken)
{
    var environment = await dbContext.Environments
        .Include(e => e.Tokens)
        .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
        ?? throw new EntityNotFoundException(nameof(Environment), request.Id);

    return new EnvironmentResponse(
        environment.Id,
        environment.Name,
        environment.Type.ToString(),
        environment.Enabled,
        environment.SortOrder,
        environment.Protected,
        environment.Tokens.Select(t => new ApiTokenResponse(
            t.Id,
            t.TokenType.ToString(),
            t.Name,
            t.CreatedAt,
            t.ExpiresAt,
            t.LastUsedAt,
            t.IsRevoked)).ToList());
}
}
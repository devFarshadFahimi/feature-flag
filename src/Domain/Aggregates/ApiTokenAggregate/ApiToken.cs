namespace Domain.Aggregates.ApiTokenAggregate;

public sealed class ApiToken : AggregateRoot<Guid>
{
    public Guid EnvironmentId { get; private set; }
    public ApiTokenType TokenType { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string? Name { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public static ApiToken Create(Guid environmentId, ApiTokenType tokenType, string tokenHash, string? name = null, DateTime? expiresAt = null)
    {
        var token = new ApiToken
        {
            Id = Guid.NewGuid(),
            EnvironmentId = environmentId,
            TokenType = tokenType,
            TokenHash = tokenHash,
            Name = name,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        return token;
    }
    private ApiToken()
    {
    }

    public void Revoke()
    {
        IsRevoked = true;
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public bool IsExpired() =>
        ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    public bool IsValid() =>
        !IsRevoked && !IsExpired();
}
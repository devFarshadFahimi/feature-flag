namespace Domain.Aggregates.UserAggregate;

public sealed class RefreshToken : Entity<int>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public string JwtId { get; private set; }
    public bool IsUsed { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private RefreshToken()
    {
    }

    public static RefreshToken Create(Guid userId, string token, string jwtId, TimeSpan expiration)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            JwtId = jwtId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiration)
        };
    }

    public void MarkAsUsed()
    {
        IsUsed = true;
    }

    public void Revoke()
    {
        IsRevoked = true;
    }

    public bool IsValid()
    {
        return !IsUsed && !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }
}
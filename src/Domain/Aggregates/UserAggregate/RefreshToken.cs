namespace Domain.Aggregates.UserAggregate;

public sealed class RefreshToken : Entity<int>
{
    public const string HashPrefix = "sha256:";

    public Guid UserId
    {
        get; private set;
    }

    public string Token { get; private set; } = string.Empty;

    public string JwtId { get; private set; } = string.Empty;

    public bool IsUsed
    {
        get; private set;
    }

    public bool IsRevoked
    {
        get; private set;
    }

    public DateTime CreatedAt
    {
        get; private set;
    }

    public DateTime ExpiresAt
    {
        get; private set;
    }

    private RefreshToken()
    {
    }

    public static RefreshToken Create(Guid userId, string token, string jwtId, TimeSpan expiration)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = Hash(token),
            JwtId = jwtId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiration)
        };
    }

    /// <summary>Stores only a marked digest of the high-entropy bearer secret.</summary>
    /// <param name = "token">The raw bearer secret.</param>
    /// <returns>The marked SHA-256 digest suitable for storage and indexed lookup.</returns>
    public static string Hash(string token)
    {
        return HashPrefix + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    }

    public void MarkAsUsed()
    {
        if (!IsValid())
        {
            throw new InvalidEntityStateException("توکن تمدید معتبر نیست.");
        }

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
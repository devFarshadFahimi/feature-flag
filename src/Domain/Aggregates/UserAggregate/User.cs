using Domain.Aggregates.UserAggregate.Events;
using Domain.Enums;

namespace Domain.Aggregates.UserAggregate;

public sealed class User : AggregateRoot<Guid>
{
    public const int FailedLoginLimit = 5;

    public const int LoginLockoutMinutes = 5;

    public string Email
    {
        get; private set;
    }

    public string? Name
    {
        get; private set;
    }

    public string PasswordHash
    {
        get; private set;
    }

    public UserRole Role
    {
        get; private set;
    }

    public bool IsActive
    {
        get; private set;
    }

    public Guid SecurityVersion { get; private set; } = Guid.NewGuid();

    public int FailedLoginAttempts
    {
        get; private set;
    }

    public DateTime? LockedUntil
    {
        get; private set;
    }

    public DateTime CreatedAt
    {
        get; private set;
    }

    public DateTime? LastLoginAt
    {
        get; private set;
    }

    private readonly List<RefreshToken> _refreshTokens = [];

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User(Guid id, string email, string passwordHash, UserRole role)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static User Create(string email, string passwordHash, UserRole role = UserRole.Viewer, string? name = null)
    {
        var user = new User(Guid.NewGuid(), email, passwordHash, role)
        {
            Name = name
        };

        user.AddEvent(new UserCreatedEvent(user.Id, email));

        return user;
    }

    public void UpdateProfile(string? name, string? email)
    {
        if (name != null)
        {
            Name = name;
        }

        if (email != null)
        {
            Email = email;
        }
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        RevokeAllRefreshTokens();
    }

    public void PromoteTo(UserRole role)
    {
        Role = role;
        RevokeAllRefreshTokens();
    }

    public void Deactivate()
    {
        IsActive = false;
        RevokeAllRefreshTokens();
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    /// <summary>Temporarily locks the account after consecutive failures; an expired lock starts a fresh attempt window.</summary>
    /// <param name = "now">The UTC time of the failed authentication attempt.</param>
    public void RecordFailedLogin(DateTime now)
    {
        if (LockedUntil > now)
        {
            return;
        }

        if (LockedUntil.HasValue)
        {
            FailedLoginAttempts = 0;
            LockedUntil = null;
        }

        FailedLoginAttempts++;

        if (FailedLoginAttempts >= FailedLoginLimit)
        {
            LockedUntil = now.AddMinutes(LoginLockoutMinutes);
        }
    }

    public RefreshToken AddRefreshToken(string token, string jwtId, TimeSpan expiration)
    {
        var refreshToken = RefreshToken.Create(Id, token, jwtId, expiration);

        _refreshTokens.Add(refreshToken);

        return refreshToken;
    }

    public void RevokeRefreshToken(string token)
    {
        var tokenHash = RefreshToken.Hash(token);
        var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.Token == tokenHash) ?? throw new InvalidEntityStateException("توکن تمدید یافت نشد.");

        refreshToken.Revoke();
    }

    /// <summary>Invalidates access tokens and revokes loaded refresh tokens; callers must also revoke any unloaded tokens.</summary>
    public void RevokeAllRefreshTokens()
    {
        SecurityVersion = Guid.NewGuid();
        foreach (var token in _refreshTokens.Where(rt => !rt.IsRevoked))
        {
            token.Revoke();
        }
    }
}
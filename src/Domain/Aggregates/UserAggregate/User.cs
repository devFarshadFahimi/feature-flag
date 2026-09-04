using Domain.Aggregates.UserAggregate.Events;
using Domain.Enums;

namespace Domain.Aggregates.UserAggregate;

public sealed class User : AggregateRoot<Guid>
{
    public string Email { get; private set; }
    public string? Name { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

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
    }

    public void PromoteTo(UserRole role)
    {
        Role = role;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public RefreshToken AddRefreshToken(string token, string jwtId, TimeSpan expiration)
    {
        var refreshToken = RefreshToken.Create(Id, token, jwtId, expiration);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeRefreshToken(string token)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.Token == token)
            ?? throw new InvalidEntityStateException("Refresh token not found");

        refreshToken.Revoke();
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(rt => !rt.IsRevoked))
        {
            token.Revoke();
        }
    }
}
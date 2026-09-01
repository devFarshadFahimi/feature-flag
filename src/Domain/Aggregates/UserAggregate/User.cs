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

    private User(Guid id, string email, string passwordHash, UserRole role)
    {
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

        user.Apply(new UserCreatedEvent(user.Id, email));
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
}
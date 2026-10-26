using BusinessMakerFramework.Shared.Abstractions.UserManagement;

namespace Application.Common.Services;

public static class CurrentActor
{
    public static Guid Id(this IUserInfoService actor) => Guid.TryParse(actor.GetClaim("sub") ?? actor.GetClaim(System.Security.Claims.ClaimTypes.NameIdentifier), out var id) ? id : throw new UnauthorizedAccessException("هویت کاربر معتبر نیست.");
    public static IQueryable<Project> VisibleProjects(this IApplicationDbContext db, IUserInfoService actor)
    {
        var id = actor.Id();

        return db.Projects.Where(p => db.Users.Any(u => u.Id == id && u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.RootAdmin || p.Members.Any(m => m.UserId == id))));
    }
}
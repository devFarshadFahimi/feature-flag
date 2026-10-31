using System.Security.Claims;
using Application.Common.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Authorization;
// Resource authorization is checked against database state, independently of cached flag configuration.
public sealed class ProjectAccess(IApplicationDbContext db) : IAsyncActionFilter
{
    /// <summary>Checks current resource permissions and serializes configuration writes with governance execution.</summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();

        if (controller == "Auth")
        {
            await next();

            return;
        }

        var ct = context.HttpContext.RequestAborted;

        if (!Guid.TryParse(context.HttpContext.User.FindFirstValue(Application.Common.Services.IdentityClaimNames.Subject) ?? context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            context.Result = Problem(401, "احراز هویت الزامی است.");

            return;
        }

        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);

        if (user is null)
        {
            context.Result = Problem(401, "نشست کاربری معتبر نیست.");

            return;
        }

        var admin = user.Role is UserRole.Admin or UserRole.RootAdmin;
        var write = !HttpMethods.IsGet(context.HttpContext.Request.Method);
        var action = context.RouteData.Values["action"]?.ToString();

        if (controller == "Evaluations" && action == "Evaluate")
        {
            write = false;
        }

        if (controller == "Metrics" && action == "Report")
        {
            write = false;
        }

        if (controller is "Users" or "Environments")
        {
            if (!admin)
            {
                context.Result = Problem(403, "دسترسی مدیر سامانه لازم است.");

                return;
            }

            var globalResult = await next();

            if (write && globalResult.Exception is null && (globalResult.Result as ObjectResult)?.StatusCode is not >= 400)
            {
                context.HttpContext.RequestServices
                    .GetRequiredService<Application.Features.Evaluations.IConfigurationSnapshots>()
                    .InvalidateAll();
            }

            return;
        }

        if (controller == "Projects" && action == "Create")
        {
            if (user.Role < UserRole.Editor)
            {
                context.Result = Problem(403, "مجوز ایجاد پروژه ندارید.");

                return;
            }

            await next();

            return;
        }

        object? Field(string name) => context.ActionArguments.Values.FirstOrDefault(v => v?.GetType().GetProperty(name) != null)?.GetType().GetProperty(name)?.GetValue(context.ActionArguments.Values.First(v => v?.GetType().GetProperty(name) != null));
        Guid? GuidValue(string name) => context.ActionArguments.TryGetValue(name, out var value) && value is Guid id ? id : Field(char.ToUpperInvariant(name[0]) + name[1..]) is Guid field ? field : null;

        var projectId = GuidValue("projectId");

        if (controller == "History" && !projectId.HasValue && !admin)
        {
            context.Result = Problem(403, "دسترسی به تاریخچه سراسری مجاز نیست.");

            return;
        }

        var featureId = GuidValue("featureId");
        var idValue = GuidValue("id");

        if (controller == "Projects")
        {
            projectId ??= idValue;
        }

        if (controller == "Features")
        {
            featureId ??= idValue;
        }

        if (featureId.HasValue)
        {
            var featureProject = await db.Features
                .Where(f => f.Id == featureId)
                .Select(f => (Guid?)f.ProjectId)
                .SingleOrDefaultAsync(ct);

            if (featureProject is null)
            {
                context.Result = Problem(404, "پرچم یافت نشد.");

                return;
            }

            if (projectId.HasValue && projectId != featureProject)
            {
                context.Result = Problem(400, "پروژه و پرچم یکسان نیستند.");

                return;
            }

            projectId = featureProject;
        }

        if (controller == "ChangeRequests" && idValue.HasValue)
        {
            var requestProject = await db.ChangeRequests
                .Where(r => r.Id == idValue)
                .Select(r => new { r.ProjectId })
                .SingleOrDefaultAsync(ct);

            if (requestProject is null)
            {
                context.Result = Problem(404, "درخواست تغییر یافت نشد.");

                return;
            }

            projectId = requestProject.ProjectId;
        }

        if (controller == "ChangeRequests" && action != "GetAll" && !projectId.HasValue && !admin)
        {
            context.Result = Problem(403, "درخواست سراسری به دسترسی مدیر نیاز دارد.");

            return;
        }

        if (controller == "Segments")
        {
            var segmentId = context.ActionArguments.TryGetValue("id", out var sid) && sid is int integer ? integer : (int?)null;

            if (segmentId.HasValue)
            {
                var segment = await db.Segments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(s => s.Id == segmentId, ct);

                if (segment is null)
                {
                    context.Result = Problem(404, "بخش یافت نشد.");

                    return;
                }

                projectId = segment.ProjectId;
            }

            if (!projectId.HasValue && write && !admin)
            {
                context.Result = Problem(403, "ویرایش بخش مشترک به دسترسی مدیر نیاز دارد.");

                return;
            }
        }

        var owner = controller == "Projects" || controller == "ChangeRequests" && action is "Approve" or "Reject" or "Apply" or "Schedule";

        if (!admin && write && user.Role < UserRole.Editor)
        {
            context.Result = Problem(403, "مجوز ویرایش ندارید.");

            return;
        }

        if (projectId.HasValue)
        {
            if (!await db.Projects.AnyAsync(p => p.Id == projectId, ct))
            {
                context.Result = Problem(404, "پروژه یافت نشد.");

                return;
            }

            if (!admin && !await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId && (!write || owner && m.Role == ProjectRole.Owner || !owner && m.Role >= ProjectRole.Member), ct))
            {
                context.Result = Problem(403, "اجازه دسترسی به این پروژه ندارید.");

                return;
            }
        }

        // Collection queries without a project are scoped in their handlers.
        // Workers use the same lock to serialize governance inspection and configuration writes.
        await using var transaction = write && !(controller == "Features" && action == "Create") ? await db.Database.BeginTransactionAsync(ct) : null;

        if (transaction is not null)
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(8610044)", ct);

            var currentUser = await db.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);

            if (currentUser is null || currentUser.SecurityVersion != user.SecurityVersion || currentUser.Role != user.Role)
            {
                context.Result = Problem(401, "نشست کاربری در زمان پردازش تغییر کرده است.");

                return;
            }

            if (!admin && projectId.HasValue && !await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId && (owner ? m.Role == ProjectRole.Owner : m.Role >= ProjectRole.Member), ct))
            {
                context.Result = Problem(403, "مجوز دسترسی پروژه تغییر کرده است.");

                return;
            }
        }

        if (write && (controller is "Features" or "FeatureEnvironments") && featureId.HasValue)
        {
            var environmentId = GuidValue("environmentId");
            var environments = db.FeatureEnvironments.Where(e => e.FeatureId == featureId && (!environmentId.HasValue || e.EnvironmentId == environmentId));

            if (await environments.AnyAsync(e => db.Environments.Any(env => env.Id == e.EnvironmentId && env.Protected) || db.ProjectEnvironmentPolicies.Any(p => p.ProjectId == projectId && p.EnvironmentId == e.EnvironmentId && p.Enabled), ct))
            {
                context.Result = Problem(409, "این پیکربندی حفاظت شده است؛ درخواست تغییر تأییدشده لازم است.");

                return;
            }
        }

        if (write && controller == "Segments" && context.ActionArguments.TryGetValue("id", out var segmentValue) && segmentValue is int protectedSegmentId)
        {
            if (await Application.Features.ChangeRequests.ConfigurationChanges
                .SegmentReferences(db, protectedSegmentId)
                .AnyAsync(s => (db.Environments.Any(e => e.Id == s.EnvironmentId && e.Protected) || db.ProjectEnvironmentPolicies.Any(p => p.ProjectId == s.Feature.ProjectId && p.EnvironmentId == s.EnvironmentId && p.Enabled)), ct))
            {
                context.Result = Problem(409, "بخش در محیط حفاظت‌شده استفاده می‌شود؛ درخواست تغییر لازم است.");

                return;
            }
        }

        var executed = await next();
        var success = executed.Exception is null && (executed.Result as ObjectResult)?.StatusCode is not >= 400;

        if (transaction is not null && success)
        {
            await transaction.CommitAsync(ct);
        }

        if (write && success)
        {
            context.HttpContext.RequestServices
                .GetRequiredService<Application.Features.Evaluations.IConfigurationSnapshots>()
                .InvalidateAll();
        }
    }

    private static ObjectResult Problem(int status, string message) => new(new ProblemDetails { Status = status, Title = message })
    {
        StatusCode = status
    };
}

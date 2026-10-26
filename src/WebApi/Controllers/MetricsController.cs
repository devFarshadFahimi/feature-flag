using Application.Common.Services;
using Application.Features.Metrics;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers;

[Authorize]
public sealed class MetricsController(IMediator mediator, IApplicationDbContext db) : ApiControllerBase(mediator)
{
    [HttpPost("impressions")]
    [RequestSizeLimit(65536)]
    public Task<IActionResult> Report([FromBody] ReportImpressionsCommand command, CancellationToken cancellationToken) => SendAsync(command, cancellationToken);
    [HttpGet]
    public async Task<IActionResult> Get(Guid projectId, Guid environmentId, DateTime from, DateTime to, string? featureName = null, string? appName = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (from.Kind != DateTimeKind.Utc || to.Kind != DateTimeKind.Utc || to <= from || to - from > TimeSpan.FromDays(31) || page is < 1 or > 100000 || pageSize is < 1 or > 200)
        {
            return BadRequest(new
            {
                Message = "بازه زمانی یا صفحه‌بندی معتبر نیست."
            });
        }


        var query = db.FlagUsage
            .AsNoTracking()
            .Where(u => u.ProjectId == projectId && u.EnvironmentId == environmentId && u.Hour >= from && u.Hour < to);

        if (featureName is not null)
        {
            query = query.Where(u => u.FeatureName == featureName);
        }

        if (appName is not null)
        {
            query = query.Where(u => u.AppName == appName);
        }


        return Ok(await query
            .OrderBy(u => u.Hour)
            .ThenBy(u => u.AppName)
            .ThenBy(u => u.FeatureName)
            .ThenBy(u => u.Variant)
            .ThenBy(u => u.Enabled)
            .ThenBy(u => u.Impression)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken));
    }

    [HttpGet("applications")]
    public async Task<IActionResult> Applications(Guid projectId, Guid environmentId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 200)
        {
            return BadRequest(new
            {
                Message = "صفحه‌بندی معتبر نیست."
            });
        }


        return Ok(await db.FlagUsage
            .AsNoTracking()
            .Where(u => u.ProjectId == projectId && u.EnvironmentId == environmentId)
            .GroupBy(u => u.AppName)
            .Select(g => new { AppName = g.Key, LastSeenAt = g.Max(u => u.LastSeenAt) })
            .OrderBy(u => u.AppName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken));
    }
}
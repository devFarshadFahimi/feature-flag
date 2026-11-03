using Application.Features.RateLimitHistory;

namespace WebApi.Controllers;

[Authorize(Policy = "AdminOnly")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RateLimitHistoryController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    public Task<IActionResult> Get([FromQuery] RateLimitHistoryFilter filter, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return SendAsync(new GetRateLimitHistoryQuery(filter, page, pageSize), cancellationToken);
    }

    [HttpGet("ips")]
    public Task<IActionResult> GetTopIps([FromQuery] RateLimitHistoryFilter filter, int limit = 50, CancellationToken cancellationToken = default)
    {
        return SendAsync(new GetTopRejectedIpsQuery(filter, limit), cancellationToken);
    }
}

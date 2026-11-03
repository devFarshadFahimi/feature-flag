using System.Net;
using Domain.Aggregates.RateLimitAggregate;

namespace Application.Features.RateLimitHistory;

public sealed record RateLimitHistoryFilter(DateTimeOffset? From = null, DateTimeOffset? To = null, string? IpAddress = null, string? Application = null, string? Environment = null, string? Method = null, string? Route = null);

public sealed record GetRateLimitHistoryQuery(RateLimitHistoryFilter Filter, int Page = 1, int PageSize = 50) : IQueryRequest<RateLimitHistoryPage>;

public sealed record GetTopRejectedIpsQuery(RateLimitHistoryFilter Filter, int Limit = 50) : IQueryRequest<List<TopRejectedIp>>;

public sealed record RateLimitHistoryPage(DateTime From, DateTime To, int Page, int PageSize, long TotalCount, List<RejectionMinute> Items);

public sealed record RejectionMinute(DateTime Minute, string IpAddress, string Application, string Environment, string Method, string Route, long RejectedRequests, DateTime FirstRejectedAt, DateTime LastRejectedAt);

public sealed record TopRejectedIp(string IpAddress, long RejectedRequests, DateTime FirstRejectedAt, DateTime LastRejectedAt);

internal sealed class GetRateLimitHistoryQueryHandler(IApplicationDbContext db) : QueryRequestHandler<GetRateLimitHistoryQuery, RateLimitHistoryPage>
{
    public override async Task<RateLimitHistoryPage> Handle(GetRateLimitHistoryQuery request, CancellationToken cancellationToken)
    {
        if (request.Page is < 1 or > 10000 || request.PageSize is < 1 or > 200)
        {
            throw new ArgumentException("صفحه‌بندی تاریخچه معتبر نیست؛ اندازه صفحه باید بین ۱ و ۲۰۰ باشد.");
        }

        var (query, from, to) = RateLimitHistoryQueries.Filter(db, request.Filter);
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(summary => summary.Minute)
            .ThenBy(summary => summary.BucketKey)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(summary => new RejectionMinute(summary.Minute, summary.IpAddress, summary.Application, summary.Environment, summary.Method, summary.Route, summary.RejectedRequests, summary.FirstRejectedAt, summary.LastRejectedAt))
            .ToListAsync(cancellationToken);

        return new RateLimitHistoryPage(from, to, request.Page, request.PageSize, total, items);
    }
}

internal sealed class GetTopRejectedIpsQueryHandler(IApplicationDbContext db) : QueryRequestHandler<GetTopRejectedIpsQuery, List<TopRejectedIp>>
{
    public override async Task<List<TopRejectedIp>> Handle(GetTopRejectedIpsQuery request, CancellationToken cancellationToken)
    {
        if (request.Limit is < 1 or > 200)
        {
            throw new ArgumentException("تعداد نشانی‌های گزارش باید بین ۱ و ۲۰۰ باشد.");
        }

        var (query, _, _) = RateLimitHistoryQueries.Filter(db, request.Filter);

        return await query
            .GroupBy(summary => summary.IpAddress)
            .OrderByDescending(group => group.Sum(summary => summary.RejectedRequests))
            .ThenBy(group => group.Key)
            .Take(request.Limit)
            .Select(group => new TopRejectedIp(group.Key, group.Sum(summary => summary.RejectedRequests), group.Min(summary => summary.FirstRejectedAt), group.Max(summary => summary.LastRejectedAt)))
            .ToListAsync(cancellationToken);
    }
}

internal static class RateLimitHistoryQueries
{
    // Reports are bounded by time and page size; input IPs use the same canonical representation as capture.
    internal static (IQueryable<RateLimitRejectionSummary> Query, DateTime From, DateTime To) Filter(IApplicationDbContext db, RateLimitHistoryFilter filter)
    {
        var now = DateTime.UtcNow;
        var to = filter.To?.UtcDateTime ?? new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc).AddMinutes(1);

        if (to < DateTime.UnixEpoch)
        {
            throw new ArgumentException("زمان پایان گزارش معتبر نیست.");
        }

        var from = filter.From?.UtcDateTime ?? to.AddHours(-24);

        if (from >= to || to - from > TimeSpan.FromDays(31))
        {
            throw new ArgumentException("بازه زمانی گزارش باید مثبت و حداکثر ۳۱ روز باشد.");
        }

        if (filter.Application?.Length > 128 || filter.Environment?.Length > 64 || filter.Method?.Length > 16 || filter.Route?.Length > 1024)
        {
            throw new ArgumentException("طول فیلترهای گزارش معتبر نیست.");
        }

        var query = db.RateLimitRejectionSummaries
            .AsNoTracking()
            .Where(summary => summary.Minute >= from && summary.Minute < to);

        if (filter.IpAddress is not null)
        {
            if (!IPAddress.TryParse(filter.IpAddress, out var address))
            {
                throw new ArgumentException("نشانی آی‌پی معتبر نیست.");
            }

            var ip = address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
            query = query.Where(summary => summary.IpAddress == ip);
        }

        if (filter.Application is not null)
        {
            query = query.Where(summary => summary.Application == filter.Application);
        }

        if (filter.Environment is not null)
        {
            query = query.Where(summary => summary.Environment == filter.Environment);
        }

        if (filter.Method is not null)
        {
            var method = filter.Method.ToUpperInvariant();
            query = query.Where(summary => summary.Method == method);
        }

        if (filter.Route is not null)
        {
            query = query.Where(summary => summary.Route == filter.Route);
        }

        return (query, from, to);
    }
}
using BusinessMakerFramework.Domain.Core.ValueObjects;

namespace Domain.ValueObjects;

public sealed class StrategyParameters : BaseValueObject<StrategyParameters>
{
    public int? RolloutPercentage { get; }
    public string? Stickiness { get; }
    public string? GroupId { get; }
    public IReadOnlyList<string> UserIds { get; }
    public IReadOnlyList<string> IpAddresses { get; }
    public IReadOnlyList<string> ApplicationNames { get; }
    public Dictionary<string, object> CustomParameters { get; }

    private StrategyParameters(
        int? rolloutPercentage,
        string? stickiness,
        string? groupId,
        IReadOnlyList<string> userIds,
        IReadOnlyList<string> ipAddresses,
        IReadOnlyList<string> applicationNames,
        Dictionary<string, object> customParameters)
    {
        if (rolloutPercentage.HasValue && (rolloutPercentage < 0 || rolloutPercentage > 100))
        {
            throw new ArgumentException("Roll-out percentage must be between 0 and 100");
        }

        RolloutPercentage = rolloutPercentage;
        Stickiness = stickiness;
        GroupId = groupId;
        UserIds = userIds;
        IpAddresses = ipAddresses;
        ApplicationNames = applicationNames;
        CustomParameters = customParameters;
    }

    public static StrategyParameters CreateDefault() =>
        new(null, null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), []);

    public static StrategyParameters CreateGradualRollout(int percentage, string stickiness = "default", string? groupId = null) =>
        new(percentage, stickiness, groupId, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), []);

    public static StrategyParameters CreateUserIds(IEnumerable<string> userIds) =>
        new(null, null, null, userIds.ToList(), Array.Empty<string>(), Array.Empty<string>(), []);

    public static StrategyParameters CreateRemoteAddress(IEnumerable<string> ips) =>
        new(null, null, null, Array.Empty<string>(), ips.ToList(), Array.Empty<string>(), []);

    public static StrategyParameters CreateApplicationHost(IEnumerable<string> apps) =>
        new(null, null, null, Array.Empty<string>(), Array.Empty<string>(), apps.ToList(), []);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RolloutPercentage ?? -1;
        yield return Stickiness ?? string.Empty;
        yield return GroupId ?? string.Empty;
        foreach (var id in UserIds)
        {
            yield return id;
        }

        foreach (var ip in IpAddresses)
        {
            yield return ip;
        }

        foreach (var app in ApplicationNames)
        {
            yield return app;
        }
    }
}

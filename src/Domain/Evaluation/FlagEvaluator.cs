using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Domain.Evaluation;

public sealed record EvaluationContext(string? UserId = null, string? SessionId = null, string? RemoteAddress = null, string? ApplicationHostname = null, DateTimeOffset? CurrentTime = null, Dictionary<string, string>? Properties = null);
public sealed record Prerequisite(Guid FeatureId, string? Variant = null);
public sealed record EvaluatedVariant(string Name, bool Enabled, IReadOnlyDictionary<string, object>? Payload);
public sealed record FlagDecision(string Name, bool Enabled, EvaluatedVariant Variant, string Reason, long Revision, Guid? MatchedStrategyId = null);
public sealed record StrategyDefinition(Guid Id, StrategyType Type, bool Enabled, int SortOrder, StrategyParameters Parameters, IReadOnlyList<Constraint> Constraints, IReadOnlyList<int> SegmentIds, IReadOnlyList<Variant> Variants);
public sealed record FlagDefinition(Guid Id, string Name, bool Enabled, bool Archived, long Revision, IReadOnlyList<StrategyDefinition> Strategies, IReadOnlyList<Variant> Variants, IReadOnlyList<Prerequisite> Prerequisites);
public sealed record ConfigurationSnapshot(Guid ProjectId, Guid EnvironmentId, bool EnvironmentEnabled, DateTimeOffset LoadedAt, IReadOnlyList<FlagDefinition> Flags, IReadOnlyDictionary<int, IReadOnlyList<Constraint>> Segments);
public static class FlagEvaluator
{
    private const string HashVersion = "v1";

    private const string RolloutPurpose = "rollout";

    private const string VariantPurpose = "variant";

    private const string DefaultStickiness = Domain.ValueObjects.StrategyParameters.DefaultStickiness;

    private const string RandomStickiness = "random";

    private const string UserIdField = "userId";

    private const string SessionIdField = "sessionId";

    public static readonly EvaluatedVariant DisabledVariant = new("disabled", false, null);

    private static readonly Regex VersionPattern = new(@"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>Evaluates current context against immutable rules, with cycle/depth guards and per-request prerequisite memoization.</summary>
    public static FlagDecision Evaluate(ConfigurationSnapshot snapshot, FlagDefinition flag, EvaluationContext context, DateTimeOffset now, bool preview = false) => Evaluate(snapshot, flag, context, now, preview, new HashSet<Guid>(), new Dictionary<Guid, (FlagDecision Decision, int Depth)>());

    private static FlagDecision Evaluate(ConfigurationSnapshot snapshot, FlagDefinition flag, EvaluationContext context, DateTimeOffset now, bool preview, HashSet<Guid> ancestors, Dictionary<Guid, (FlagDecision Decision, int Depth)> memo)
    {
        if (ancestors.Contains(flag.Id) || ancestors.Count >= 20)
        {
            return new(flag.Name, false, DisabledVariant, "dependency_cycle_or_depth", flag.Revision);
        }

        if (memo.TryGetValue(flag.Id, out var cached))
        {
            return cached.Decision.Enabled && ancestors.Count + cached.Depth > 20 ? new(flag.Name, false, DisabledVariant, "dependency_cycle_or_depth", flag.Revision) : cached.Decision;
        }

        var decision = EvaluateCore(snapshot, flag, context, now, preview, ancestors, memo);
        var depth = 1 + flag.Prerequisites
            .Select(p => memo.TryGetValue(p.FeatureId, out var parent) ? parent.Depth : 0)
            .DefaultIfEmpty(0)
            .Max();

        memo[flag.Id] = (decision, depth);

        return decision;
    }

    private static FlagDecision EvaluateCore(ConfigurationSnapshot snapshot, FlagDefinition flag, EvaluationContext context, DateTimeOffset now, bool preview, HashSet<Guid> ancestors, Dictionary<Guid, (FlagDecision Decision, int Depth)> memo)
    {
        FlagDecision Off(string reason) => new(flag.Name, false, DisabledVariant, reason, flag.Revision);

        if (!snapshot.EnvironmentEnabled)
        {
            return Off("environment_disabled");
        }

        if (flag.Archived)
        {
            return Off("archived");
        }

        if (!flag.Enabled)
        {
            return Off("disabled");
        }

        if (ancestors.Count >= 20 || !ancestors.Add(flag.Id))
        {
            return Off("dependency_cycle_or_depth");
        }

        try
        {
            foreach (var dependency in flag.Prerequisites)
            {
                var parent = snapshot.Flags.FirstOrDefault(f => f.Id == dependency.FeatureId);

                if (parent is null)
                {
                    return Off("prerequisite_failed");
                }

                var decision = Evaluate(snapshot, parent, context, now, preview, ancestors, memo);

                if (!decision.Enabled || dependency.Variant is not null && decision.Variant.Name != dependency.Variant)
                {
                    return Off("prerequisite_failed");
                }
            }

            foreach (var strategy in flag.Strategies
                .Where(s => s.Enabled)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Id))
            {
                if (!strategy.Constraints.All(c => Matches(c, context, preview ? context.CurrentTime ?? now : now)))
                {
                    continue;
                }

                if (!strategy.SegmentIds.All(id => snapshot.Segments.TryGetValue(id, out var constraints) && constraints.All(c => Matches(c, context, preview ? context.CurrentTime ?? now : now))))
                {
                    continue;
                }

                if (!MatchesStrategy(snapshot.ProjectId, flag.Name, strategy, context))
                {
                    continue;
                }

                var variants = strategy.Variants.Count > 0 ? strategy.Variants : flag.Variants;
                var variant = SelectVariant(snapshot.ProjectId, flag.Name, strategy.Parameters, variants, context);

                return new(flag.Name, true, variant, "strategy_matched", flag.Revision, preview ? strategy.Id : null);
            }

            return Off("no_matching_strategy");
        }
        finally
        {
            ancestors.Remove(flag.Id);
        }
    }

    public static void ValidatePrerequisites(IReadOnlyDictionary<Guid, IReadOnlyList<Prerequisite>> graph)
    {
        var completed = new Dictionary<Guid, int>();
        var visiting = new HashSet<Guid>();

        int Depth(Guid id)
        {
            if (completed.TryGetValue(id, out var cached))
            {
                return cached;
            }

            if (!graph.TryGetValue(id, out var parents) || !visiting.Add(id) || visiting.Count > 20)
            {
                throw new ArgumentException("پیش‌نیاز یافت نشد یا دارای چرخه یا عمق بیش از ۲۰ است.");
            }

            if (parents.Count > 20 || parents
                .Select(p => p.FeatureId)
                .Distinct()
                .Count() != parents.Count)
            {
                throw new ArgumentException("فهرست پیش‌نیازها باید یکتا و دارای حداکثر ۲۰ عضو باشد.");
            }

            var depth = 1 + parents
                .Select(p => Depth(p.FeatureId))
                .DefaultIfEmpty(0)
                .Max();

            visiting.Remove(id);

            if (depth > 20)
            {
                throw new ArgumentException("عمق پیش‌نیازها بیش از ۲۰ است.");
            }

            completed[id] = depth;

            return depth;
        }

        foreach (var id in graph.Keys)
        {
            _ = Depth(id);
        }
    }

    public static bool Matches(Constraint constraint, EvaluationContext context, DateTimeOffset now)
    {
        if (!Enum.IsDefined(constraint.Operator))
        {
            return false;
        }

        var op = constraint.Operator;

        if (op is ConstraintOperator.AlwaysTrue or ConstraintOperator.AlwaysFalse)
        {
            return (op == ConstraintOperator.AlwaysTrue) != constraint.Inverted;
        }

        var value = ContextValue(context, constraint.ContextName, now);

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var comparison = constraint.CaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        bool match;

        if (op is >= ConstraintOperator.NumEq and <= ConstraintOperator.NumLte)
        {
            if (constraint.Values.Count != 1 || !decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var actual) || !decimal.TryParse(constraint.Values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var expected))
            {
                return false;
            }

            match = op switch
            {
                ConstraintOperator.NumEq => actual == expected,
                ConstraintOperator.NumGt => actual > expected,
                ConstraintOperator.NumGte => actual >= expected,
                ConstraintOperator.NumLt => actual < expected,
                _ => actual <= expected
            };
        }
        else if (op is ConstraintOperator.DateAfter or ConstraintOperator.DateBefore)
        {
            if (constraint.Values.Count != 1 || !ParseDate(value, out var actual) || !ParseDate(constraint.Values[0], out var expected))
            {
                return false;
            }

            match = op == ConstraintOperator.DateAfter ? actual > expected : actual < expected;
        }
        else if (op is >= ConstraintOperator.SemverEq and <= ConstraintOperator.SemverLte)
        {
            if (constraint.Values.Count != 1 || !TryCompareVersions(value, constraint.Values[0], out var order))
            {
                return false;
            }

            match = op switch
            {
                ConstraintOperator.SemverEq => order == 0,
                ConstraintOperator.SemverGt => order > 0,
                ConstraintOperator.SemverGte => order >= 0,
                ConstraintOperator.SemverLt => order < 0,
                _ => order <= 0
            };
        }
        else
        {
            if (constraint.Values.Count == 0)
            {
                return false;
            }

            match = op switch
            {
                ConstraintOperator.In => constraint.Values.Any(v => string.Equals(value, v, comparison)),
                ConstraintOperator.NotIn => constraint.Values.All(v => !string.Equals(value, v, comparison)),
                ConstraintOperator.StrStartsWith => constraint.Values.Any(v => value.StartsWith(v, comparison)),
                ConstraintOperator.StrEndsWith => constraint.Values.Any(v => value.EndsWith(v, comparison)),
                ConstraintOperator.StrContains => constraint.Values.Any(v => value.Contains(v, comparison)),
                _ => false
            };
        }

        return match != constraint.Inverted;
    }

    public static void ValidateConstraint(string name, ConstraintOperator op, IReadOnlyList<string> values)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128 || !Enum.IsDefined(op) || values.Any(v => v is null || v.Length > 1024))
        {
            throw new ArgumentException("نام، عملگر یا مقدار قید معتبر نیست.");
        }

        if (op is ConstraintOperator.AlwaysTrue or ConstraintOperator.AlwaysFalse)
        {
            return;
        }

        if (op is >= ConstraintOperator.NumEq and <= ConstraintOperator.NumLte && (values.Count != 1 || !decimal.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
        {
            throw new ArgumentException("قید عددی به یک مقدار عددی معتبر نیاز دارد.");
        }

        if (op is ConstraintOperator.DateAfter or ConstraintOperator.DateBefore && (values.Count != 1 || !ParseDate(values[0], out _)))
        {
            throw new ArgumentException("تاریخ قید باید با منطقه زمانی مشخص ارسال شود.");
        }

        if (op is >= ConstraintOperator.SemverEq and <= ConstraintOperator.SemverLte && (values.Count != 1 || !TryCompareVersions(values[0], values[0], out _)))
        {
            throw new ArgumentException("نسخه معنایی قید معتبر نیست.");
        }

        if (values.Count == 0)
        {
            throw new ArgumentException("حداقل یک مقدار برای قید لازم است.");
        }
    }

    /// <summary>Produces a language-independent bucket from length-prefixed UTF-8 fields and a SHA-256 digest.</summary>
    public static int Bucket(Guid projectId, string group, string field, string value, string purpose)
    {
        using var stream = new MemoryStream();

        foreach (var part in new[]
        {
            HashVersion,
            projectId.ToString("D"),
            group,
            field,
            value,
            purpose
        }

        )
        {
            var bytes = Encoding.UTF8.GetBytes(part);
            Span<byte> length = stackalloc byte[4];

            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            stream.Write(length);
            stream.Write(bytes);
        }

        return (int)(BinaryPrimitives.ReadUInt64BigEndian(SHA256.HashData(stream.ToArray())) % 10000);
    }

    private static bool MatchesStrategy(Guid projectId, string name, StrategyDefinition strategy, EvaluationContext context)
    {
        var p = strategy.Parameters;

        return strategy.Type switch
        {
            StrategyType.Default => true,
            StrategyType.UserWithId => !string.IsNullOrEmpty(context.UserId) && p.UserIds.Contains(context.UserId, StringComparer.Ordinal),
            StrategyType.RemoteAddress => p.IpAddresses.Any(range => IpMatches(context.RemoteAddress, range)),
            StrategyType.ApplicationHost => !string.IsNullOrEmpty(context.ApplicationHostname) && p.ApplicationNames.Contains(context.ApplicationHostname, StringComparer.Ordinal),
            StrategyType.GradualRollout or StrategyType.FlexibleRollout => Rollout(),
            _ => false
        };

        bool Rollout()
        {
            if (p.RolloutPercentage is null or <= 0 or > 100)
            {
                return false;
            }

            if (p.RolloutPercentage == 100)
            {
                return true;
            }

            var (field, value) = Stickiness(p.Stickiness, context);

            return value is not null && Bucket(projectId, p.GroupId ?? name, field, value, RolloutPurpose) < p.RolloutPercentage * 100;
        }
    }

    private static EvaluatedVariant SelectVariant(Guid projectId, string name, StrategyParameters parameters, IReadOnlyList<Variant> variants, EvaluationContext context)
    {
        if (variants.Count == 0 || variants.Sum(v => (long)v.Weight) != 1000)
        {
            return DisabledVariant;
        }

        // A single shared stickiness key prevents overlapping or unassigned weighted ranges.
        var (field, value) = Stickiness(variants[0].Stickiness ?? parameters.Stickiness, context);

        if (value is null)
        {
            return DisabledVariant;
        }

        var bucket = Bucket(projectId, parameters.GroupId ?? name, field, value, VariantPurpose) / 10;
        var upper = 0;

        foreach (var variant in variants.OrderBy(v => v.Name, StringComparer.Ordinal))
        {
            upper += variant.Weight;

            if (bucket < upper)
            {
                return new(variant.Name, true, variant.Payload);
            }
        }

        return DisabledVariant;
    }

    private static (string Field, string? Value) Stickiness(string? field, EvaluationContext context)
    {
        if (field == RandomStickiness)
        {
            return (RandomStickiness, RandomNumberGenerator
                .GetInt32(int.MaxValue)
                .ToString(CultureInfo.InvariantCulture));
        }

        if (string.IsNullOrEmpty(field) || field == DefaultStickiness)
        {
            field = !string.IsNullOrEmpty(context.UserId) ? UserIdField : SessionIdField;
        }

        var value = ContextValue(context, field, DateTimeOffset.UtcNow);

        return (field, string.IsNullOrEmpty(value) ? null : value);
    }

    private static string? ContextValue(EvaluationContext context, string name, DateTimeOffset now) => name switch
    {
        UserIdField => context.UserId,
        SessionIdField => context.SessionId,
        "remoteAddress" => context.RemoteAddress,
        "applicationHostname" => context.ApplicationHostname,
        "currentTime" => now
        .ToUniversalTime()
        .ToString("O"),
        _ => context.Properties?.GetValueOrDefault(name)
    };

    public static bool IpMatches(string? address, string range)
    {
        if (!IPAddress.TryParse(address, out var ip))
        {
            return false;
        }

        var parts = range.Split('/');

        if (parts.Length > 2 || !IPAddress.TryParse(parts[0], out var network))
        {
            return false;
        }

        var bytes = ip.GetAddressBytes();
        var networkBytes = network.GetAddressBytes();

        if (bytes.Length != networkBytes.Length)
        {
            return false;
        }

        var bits = bytes.Length * 8;

        if (parts.Length == 2 && (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out bits) || bits < 0 || bits > bytes.Length * 8))
        {
            return false;
        }

        for (var index = 0; index < bytes.Length; index++)
        {
            var mask = (byte)(255 << (8 - Math.Clamp(bits - index * 8, 0, 8)));

            if ((bytes[index] & mask) != (networkBytes[index] & mask))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParseDate(string value, out DateTimeOffset result) => DateTimeOffset.TryParseExact(value, ["O", "yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"], CultureInfo.InvariantCulture, DateTimeStyles.None, out result) && (value.EndsWith('Z') || Regex.IsMatch(value, @"[+-][0-9]{2}:[0-9]{2}$", RegexOptions.CultureInvariant));

    private static bool TryCompareVersions(string left, string right, out int order)
    {
        order = 0;

        var a = VersionPattern.Match(left);
        var b = VersionPattern.Match(right);

        if (!a.Success || !b.Success)
        {
            return false;
        }

        for (var i = 1; i <= 3; i++)
        {
            order = CompareNumber(a.Groups[i].Value, b.Groups[i].Value);

            if (order != 0)
            {
                break;
            }
        }

        var apre = a.Groups[4].Value;
        var bpre = b.Groups[4].Value;

        if (!ValidPrerelease(apre) || !ValidPrerelease(bpre))
        {
            return false;
        }

        if (order != 0)
        {
            return true;
        }

        if (apre.Length == 0 || bpre.Length == 0)
        {
            order = apre.Length == bpre.Length ? 0 : apre.Length == 0 ? 1 : -1;

            return true;
        }

        var ap = apre.Split('.');
        var bp = bpre.Split('.');

        for (var i = 0; i < Math.Min(ap.Length, bp.Length); i++)
        {
            var an = ap[i].All(char.IsAsciiDigit);
            var bn = bp[i].All(char.IsAsciiDigit);

            order = an && bn ? CompareNumber(ap[i], bp[i]) : an != bn ? an ? -1 : 1 : string.CompareOrdinal(ap[i], bp[i]);

            if (order != 0)
            {
                return true;
            }
        }

        order = ap.Length.CompareTo(bp.Length);

        return true;
    }

    private static bool ValidPrerelease(string value) => value.Length == 0 || value
        .Split('.')
        .All(p => !p.All(char.IsAsciiDigit) || p.Length == 1 || p[0] != '0');

    private static int CompareNumber(string a, string b) => a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);
}
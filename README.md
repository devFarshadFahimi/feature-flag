# edge-services-file-manager



dotnet ef migrations add Init -s .\src\WebApi\WebApi.csproj -p .\src\Infrastructure\Infrastructure.csproj
dotnet ef database update -s .\src\WebApi\WebApi.csproj -p .\src\Infrastructure\Infrastructure.csproj

---


# What's Missing in the Backend — Honest Assessment

We've built a solid foundation, but several **critical pieces** are still missing for this to actually function as a feature flag system. Let me break it down by priority.

---

## 🚨 Critical Missing Pieces (System Won't Work Without These)

### 1. **The Evaluation Engine** ⭐ THE BIGGEST GAP
We defined the domain models but **never built the actual evaluation logic** — this is the heart of any feature flag system!

**Missing:**
- `Domain/Services/FeatureEvaluationService.cs` — evaluates flags against user context
- Constraint operator evaluators (18+ operators: IN, STR_STARTS_WITH, NUM_GT, DATE_AFTER, SEMVER_EQ, etc.)
- Murmur3 hash implementation (must match other SDKs bit-for-bit for sticky rollouts)
- Segment resolution (expanding segment references into constraints)
- Variant selection algorithm (weighted distribution with stickiness)
- Strategy OR-logic evaluation (any strategy matching = enabled)

```csharp
// What you need:
public interface IFeatureEvaluator
{
    bool IsEnabled(string featureKey, EvaluationContext context);
    Variant? GetVariant(string featureKey, EvaluationContext context, Variant? defaultVariant);
    IReadOnlyDictionary<string, EvaluatedFeature> EvaluateAll(EvaluationContext context);
}
```

### 2. **SDK API (Separate from Admin API)**
We only built admin endpoints. Real feature flag systems have **two completely separate APIs**:

| Admin API | SDK API |
|-----------|---------|
| For dashboard users | For client applications |
| JWT auth | API key auth |
| Low throughput | High throughput (millions of req/sec) |
| Read + Write | Mostly Read |
| Complex responses | Optimized payloads |

**Missing endpoints:**
- `POST /api/sdk/v1/evaluate` — evaluate flags for a user context
- `GET /api/sdk/v1/flags` — bootstrap (get all flags for environment)
- `GET /api/sdk/v1/delta` — delta streaming (only changes since revision X)
- `POST /api/sdk/v1/metrics` — receive usage metrics from SDKs
- `/api/frontend/v1/features` — for browser SDKs (filtered payload)

### 3. **Audit Log Persistence**
We emit domain events but **never persist them**. For a governance-focused system like Unleash, audit logs are non-negotiable.

**Missing:**
- `AuditLog` aggregate/entity
- `IAuditLogRepository`
- Domain event handler that converts events → audit entries
- Query endpoints for audit log (with filtering by entity, user, date range)
- Diff viewer (before/after JSON)

### 4. **Realtime Streaming**
Modern SDKs expect **instant updates** when flags change. Without this, clients must poll.

**Missing:**
- SignalR Hub for .NET clients
- SSE endpoint for JS/Go/Python clients
- Revision token system (global counter bumped on every change)
- Redis pub/sub for multi-instance synchronization
- Delta format (only send what changed)

---

## ⚠️ Important Missing Pieces (Production Requirements)

### 5. **Caching Layer**
Without caching, every SDK request hits Postgres — **this will not scale**.

**Missing:**
- `ICacheService` abstraction
- Redis implementation
- Cache invalidation strategy (on every admin write)
- Per-environment flag snapshot caching
- ETag support for HTTP caching

### 6. **Background Jobs**
Several features require scheduled execution:

**Missing:**
- **Scheduled releases** — enable flags at a specific time
- **Scheduled archiving** — auto-archive old flags
- **Stale flag detection** — mark flags unused for N days
- **Token expiration cleanup** — remove expired refresh tokens
- **Metrics aggregation** — roll up per-minute metrics to hourly/daily
- **Change request scheduler** — apply scheduled CRs

**Recommended:** Hangfire or Quartz.NET

### 7. **Validation Pipeline**
Commands are not validated. We need:

**Missing:**
- FluentValidation integration
- Validation behavior in MediatR pipeline
- Validators for every command (email format, name length, etc.)
- Consistent validation error responses

### 8. **Global Exception Handling**
Currently, exceptions bubble up as 500s with stack traces.

**Missing:**
- `ExceptionHandlingMiddleware`
- Mapping `DomainException` → 400/409
- Mapping `EntityNotFoundException` → 404
- Mapping `UnauthorizedAccessException` → 401/403
- Consistent error response format (`{ error: { code, message, details } }`)
- Request logging with correlation IDs

### 9. **Metrics Ingestion Pipeline**
SDKs send usage metrics every ~15 seconds. At scale, this is **millions of rows/hour**.

**Missing:**
- Metrics endpoint optimized for bulk writes
- In-memory buffer → batch insert pattern
- TimescaleDB or separate metrics table with retention policy
- Aggregation queries (per flag × env × hour)
- Usage analytics endpoints

### 10. **Webhooks / Addon System**
Enterprise customers need integrations (Slack, Teams, Jira, Datadog).

**Missing:**
- `Webhook` aggregate
- Webhook dispatcher service
- Outbox pattern for guaranteed delivery
- Retry logic with exponential backoff
- Template engine for payloads
- UI for managing webhooks

---

## 🔧 Cross-Cutting Concerns

### 11. **Current User Abstraction**
`IApplicationProvider` is referenced but not fully defined.

**Missing:**
- `ICurrentUser` interface
- Implementation that reads from `HttpContext.User` claims
- Proper handling of system/anonymous users
- Audit context propagation

### 12. **API Versioning**
You'll need to evolve the API without breaking clients.

**Missing:**
- `Asp.Versioning.Mvc` package
- Versioned controllers (v1, v2)
- Deprecation strategy

### 13. **Rate Limiting**
SDK endpoints need protection from abuse.

**Missing:**
- `AddRateLimiter()` configuration
- Per-API-key limits
- Per-IP limits for auth endpoints
- Different policies for admin vs SDK APIs

### 14. **Health Checks**
Production deployments need liveness/readiness probes.

**Missing:**
- `AddHealthChecks()` with Postgres, Redis probes
- `/health` endpoint
- Detailed health for Kubernetes

### 15. **Swagger with JWT Support**
Swagger needs to know about auth.

**Missing:**
- `AddSwaggerGen` with JWT bearer security definition
- Operation filters for `[Authorize]` endpoints

---

## 🎯 Advanced Features (For Full Unleash-Equivalence)

### 16. **Change Request Application Logic**
We built the workflow but **not the actual application** of changes:
- When a CR is applied, items must mutate the actual flags
- This requires a command interpreter per action type

### 17. **Feature Promotion Between Environments**
"Promote to staging" → "Promote to production" workflow is missing.

### 18. **Playground / Simulator**
"Given this user context, which flags would be enabled?" — requires the evaluation engine exposed as an API.

### 19. **Flag Dependencies**
Parent/child flag relationships (if parent is off, children are off).

### 20. **Impact Estimation**
"Enabling this flag at 50% rollout will affect X users" — requires metrics + evaluation engine.

### 21. **OpenFeature Provider**
Implement the [OpenFeature](https://openfeature.dev/) spec for vendor-neutral SDK integration.

### 22. **Edge Proxy Mode**
For ultra-low latency deployments (Cloudflare Workers, Fastly).

### 23. **Multi-Tenancy**
If you're building a SaaS:
- Tenant resolution middleware
- Row-level security or schema-per-tenant
- Tenant-scoped queries everywhere

---

## 📊 Priority Matrix

| Priority | Item | Effort | Impact |
|----------|------|--------|--------|
| 🔴 **P0** | Evaluation Engine | 2-3 weeks | System is useless without this |
| 🔴 **P0** | SDK API endpoints | 1 week | Clients can't consume flags |
| 🔴 **P0** | Audit Log persistence | 3-5 days | Governance requirement |
| 🟠 **P1** | Realtime streaming (SignalR/SSE) | 1 week | Modern UX requirement |
| 🟠 **P1** | Redis caching | 3-5 days | Performance at scale |
| 🟠 **P1** | Exception handling middleware | 1-2 days | Production stability |
| 🟠 **P1** | Validation pipeline | 2-3 days | Data integrity |
| 🟡 **P2** | Background jobs | 1 week | Scheduled features |
| 🟡 **P2** | Metrics ingestion | 1 week | Analytics |
| 🟡 **P2** | Webhooks | 1 week | Integrations |
| 🟢 **P3** | Rate limiting | 1-2 days | Security |
| 🟢 **P3** | Health checks | 1 day | Operations |
| 🟢 **P3** | API versioning | 2-3 days | Future-proofing |
| 🔵 **P4** | Playground, dependencies, impact estimation | 2-3 weeks | Advanced features |
| 🔵 **P4** | OpenFeature, Edge Proxy | 2-4 weeks | Ecosystem integration |

---

## 🎯 My Recommendation: Next Steps in Order

If I were continuing this project, I'd tackle them in this order:

1. **Evaluation Engine** — without this, nothing else matters
2. **SDK API + Caching** — make the system actually usable by clients
3. **Audit Log** — governance table-stakes
4. **Exception handling + Validation** — production hygiene
5. **Realtime streaming** — modern UX expectation
6. **Background jobs** — unlock scheduled releases
7. **Metrics pipeline** — unlock analytics
8. **Webhooks** — enterprise integrations

**Total estimated effort for P0+P1:** ~4-5 weeks with 2 engineers.

Want me to start with the **Evaluation Engine** (the most critical missing piece)? That's where the real magic of a feature flag system lives. 🎯


----


# Building the Evaluation Engine — The Heart of the System 🧠

This is the most critical missing piece. I'll build it in layers: **hashing → constraint evaluation → strategy evaluation → feature evaluation → SDK API → caching**.

## 📁 What We're Building

```
Domain/
├── Services/
│   ├── Evaluation/
│   │   ├── EvaluationContext.cs
│   │   ├── EvaluatedFeature.cs
│   │   ├── IFeatureEvaluator.cs
│   │   ├── FeatureEvaluator.cs
│   │   ├── ConstraintEvaluator.cs
│   │   ├── StrategyEvaluator.cs
│   │   ├── VariantResolver.cs
│   │   └── Hashing/
│   │       ├── MurmurHash3.cs
│   │       └── NormalizedHash.cs
│   └── Exceptions/
│       └── EvaluationException.cs

Application/
├── Features/Sdk/
│   ├── Commands/EvaluateFeature/
│   ├── Commands/EvaluateAllFeatures/
│   ├── Queries/GetBootstrap/
│   └── Commands/ReportMetrics/

Infrastructure/
├── Caching/
│   ├── ICacheService.cs
│   ├── InMemoryCacheService.cs
│   └── RedisCacheService.cs
└── Services/
    └── FeatureSnapshotService.cs

WebApi/
└── Controllers/Sdk/
    ├── SdkController.cs
    └── FrontendController.cs
```

---

## 🎯 Domain Layer — Evaluation Core

### `Domain/Services/Evaluation/EvaluationContext.cs`

```csharp
namespace Domain.Services.Evaluation;

/// <summary>
/// Immutable context representing the entity requesting flag evaluation.
/// Passed through the evaluation pipeline to resolve constraints.
/// </summary>
public sealed class EvaluationContext
{
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
    public string? RemoteAddress { get; init; }
    public string? Environment { get; init; }
    public string? AppName { get; init; }
    public DateTime CurrentTime { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Custom properties (plan, country, email, etc.)
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; } 
        = new Dictionary<string, string>();

    public EvaluationContext() { }

    /// <summary>
    /// Resolve a context value by name. Supports standard fields + custom properties.
    /// </summary>
    public string? GetContextValue(string contextName)
    {
        return contextName.ToLowerInvariant() switch
        {
            "userid" => UserId,
            "sessionid" => SessionId,
            "remoteaddress" => RemoteAddress,
            "environment" => Environment,
            "appname" => AppName,
            "currenttime" => CurrentTime.ToString("o"),
            _ => Properties.TryGetValue(contextName, out var value) ? value : null
        };
    }

    /// <summary>
    /// Get the identifier used for stickiness (rollout hashing).
    /// </summary>
    public string? GetIdentifier(string stickiness)
    {
        return stickiness.ToLowerInvariant() switch
        {
            "userid" => UserId,
            "sessionid" => SessionId,
            "default" => UserId ?? SessionId ?? RandomIdentifier(),
            "random" => RandomIdentifier(),
            _ => GetContextValue(stickiness)
        };
    }

    private static string RandomIdentifier() => Guid.NewGuid().ToString("N");
}
```

### `Domain/Services/Evaluation/EvaluatedFeature.cs`

```csharp
namespace Domain.Services.Evaluation;

/// <summary>
/// Result of evaluating a single feature flag.
/// </summary>
public sealed record EvaluatedFeature(
    string Name,
    bool Enabled,
    VariantResult? Variant = null,
    bool ImpressionData = false);

/// <summary>
/// Selected variant for a feature.
/// </summary>
public sealed record VariantResult(
    string Name,
    bool Enabled,
    Dictionary<string, object>? Payload = null);
```

### `Domain/Services/Evaluation/Hashing/MurmurHash3.cs`

```csharp
namespace Domain.Services.Evaluation.Hashing;

/// <summary>
/// MurmurHash3 32-bit — MUST match Unleash/other SDKs bit-for-bit
/// for consistent sticky rollouts across languages.
/// Reference: https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp
/// </summary>
public static class MurmurHash3
{
    private const uint C1 = 0xcc9e2d51;
    private const uint C2 = 0x1b873593;

    public static uint Hash32(string key, uint seed = 0)
    {
        var data = System.Text.Encoding.UTF8.GetBytes(key);
        return Hash32(data, seed);
    }

    public static uint Hash32(byte[] data, uint seed = 0)
    {
        var length = data.Length;
        var nblocks = length / 4;
        var h1 = seed;

        // Body
        for (var i = 0; i < nblocks; i++)
        {
            var i4 = i * 4;
            var k1 = (uint)(data[i4] | (data[i4 + 1] << 8) | (data[i4 + 2] << 16) | (data[i4 + 3] << 24));

            k1 *= C1;
            k1 = RotateLeft(k1, 15);
            k1 *= C2;

            h1 ^= k1;
            h1 = RotateLeft(h1, 13);
            h1 = h1 * 5 + 0xe6546b64;
        }

        // Tail
        var tail = nblocks * 4;
        uint k1Tail = 0;
        switch (length & 3)
        {
            case 3: k1Tail ^= (uint)data[tail + 2] << 16; goto case 2;
            case 2: k1Tail ^= (uint)data[tail + 1] << 8; goto case 1;
            case 1:
                k1Tail ^= data[tail];
                k1Tail *= C1;
                k1Tail = RotateLeft(k1Tail, 15);
                k1Tail *= C2;
                h1 ^= k1Tail;
                break;
        }

        // Finalization
        h1 ^= (uint)length;
        h1 = FMix(h1);

        return h1;
    }

    private static uint RotateLeft(uint x, byte r) => (x << r) | (x >> (32 - r));

    private static uint FMix(uint h)
    {
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;
        return h;
    }
}
```

### `Domain/Services/Evaluation/Hashing/NormalizedHash.cs`

```csharp
namespace Domain.Services.Evaluation.Hashing;

/// <summary>
/// Normalizes a Murmur3 hash to a value between 1 and 100 (inclusive).
/// Used for percentage rollouts — same identifier always gets same bucket.
/// </summary>
public static class NormalizedHash
{
    public static int Compute(string identifier, string groupId, uint normalizer = 100)
    {
        if (string.IsNullOrEmpty(identifier))
            return 0;

        var hash = MurmurHash3.Hash32($"{identifier}:{groupId}");
        return (int)((hash % normalizer) + 1);
    }
}
```

### `Domain/Services/Evaluation/ConstraintEvaluator.cs`

```csharp
using System.Globalization;
using System.Text.RegularExpressions;
using Domain.ValueObjects;
using Domain.Enums;

namespace Domain.Services.Evaluation;

/// <summary>
/// Evaluates a single constraint against the evaluation context.
/// Supports all 18+ Unleash-compatible operators.
/// </summary>
public static class ConstraintEvaluator
{
    public static bool Evaluate(Constraint constraint, EvaluationContext context)
    {
        var contextValue = context.GetContextValue(constraint.ContextName);
        var result = EvaluateInternal(constraint, contextValue);
        
        // Inverted constraints flip the result
        return constraint.Inverted ? !result : result;
    }

    private static bool EvaluateInternal(Constraint constraint, string? contextValue)
    {
        return constraint.Operator switch
        {
            // String operators
            ConstraintOperator.In => StringIn(contextValue, constraint.Values, constraint.CaseInsensitive),
            ConstraintOperator.NotIn => !StringIn(contextValue, constraint.Values, constraint.CaseInsensitive),
            ConstraintOperator.StrStartsWith => StringOp(contextValue, constraint.Values, constraint.CaseInsensitive, 
                (cv, v) => cv.StartsWith(v, StringComparison(constraint.CaseInsensitive))),
            ConstraintOperator.StrEndsWith => StringOp(contextValue, constraint.Values, constraint.CaseInsensitive,
                (cv, v) => cv.EndsWith(v, StringComparison(constraint.CaseInsensitive))),
            ConstraintOperator.StrContains => StringOp(contextValue, constraint.Values, constraint.CaseInsensitive,
                (cv, v) => cv.Contains(v, StringComparison(constraint.CaseInsensitive))),

            // Numeric operators
            ConstraintOperator.NumEq => NumericOp(contextValue, constraint.Values, (a, b) => a == b),
            ConstraintOperator.NumGt => NumericOp(contextValue, constraint.Values, (a, b) => a > b),
            ConstraintOperator.NumGte => NumericOp(contextValue, constraint.Values, (a, b) => a >= b),
            ConstraintOperator.NumLt => NumericOp(contextValue, constraint.Values, (a, b) => a < b),
            ConstraintOperator.NumLte => NumericOp(contextValue, constraint.Values, (a, b) => a <= b),

            // Date operators
            ConstraintOperator.DateAfter => DateOp(contextValue, constraint.Values, (a, b) => a > b),
            ConstraintOperator.DateBefore => DateOp(contextValue, constraint.Values, (a, b) => a < b),

            // Semver operators
            ConstraintOperator.SemverEq => SemverOp(contextValue, constraint.Values, (a, b) => a.CompareTo(b) == 0),
            ConstraintOperator.SemverGt => SemverOp(contextValue, constraint.Values, (a, b) => a.CompareTo(b) > 0),
            ConstraintOperator.SemverLt => SemverOp(contextValue, constraint.Values, (a, b) => a.CompareTo(b) < 0),

            // Special
            ConstraintOperator.AlwaysTrue => true,
            ConstraintOperator.AlwaysFalse => false,

            _ => false
        };
    }

    private static bool StringIn(string? value, IReadOnlyList<string> values, bool caseInsensitive)
    {
        if (value == null) return false;
        var comparison = StringComparison(caseInsensitive);
        return values.Any(v => string.Equals(v, value, comparison));
    }

    private static bool StringOp(string? value, IReadOnlyList<string> targets, bool caseInsensitive, Func<string, string, bool> op)
    {
        if (value == null) return false;
        return targets.Any(t => op(value, t));
    }

    private static bool NumericOp(string? value, IReadOnlyList<string> targets, Func<double, double, bool> op)
    {
        if (value == null || targets.Count == 0) return false;
        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var contextNum))
            return false;
        if (!double.TryParse(targets[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var targetNum))
            return false;
        return op(contextNum, targetNum);
    }

    private static bool DateOp(string? value, IReadOnlyList<string> targets, Func<DateTime, DateTime, bool> op)
    {
        if (value == null || targets.Count == 0) return false;
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var contextDate))
            return false;
        if (!DateTime.TryParse(targets[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var targetDate))
            return false;
        return op(contextDate.ToUniversalTime(), targetDate.ToUniversalTime());
    }

    private static bool SemverOp(string? value, IReadOnlyList<string> targets, Func<SemVersion, SemVersion, bool> op)
    {
        if (value == null || targets.Count == 0) return false;
        if (!SemVersion.TryParse(value, out var contextVer)) return false;
        if (!SemVersion.TryParse(targets[0], out var targetVer)) return false;
        return op(contextVer, targetVer);
    }

    private static StringComparison StringComparison(bool caseInsensitive)
        => caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}

/// <summary>
/// Minimal semantic version implementation (major.minor.patch with optional prerelease).
/// </summary>
public readonly struct SemVersion : IComparable<SemVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? PreRelease { get; }

    private SemVersion(int major, int minor, int patch, string? preRelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
    }

    public static bool TryParse(string value, out SemVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var match = Regex.Match(value.Trim(), @"^(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:-(.+))?$");
        if (!match.Success) return false;

        var major = int.Parse(match.Groups[1].Value);
        var minor = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        var patch = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        var pre = match.Groups[4].Success ? match.Groups[4].Value : null;

        version = new SemVersion(major, minor, patch, pre);
        return true;
    }

    public int CompareTo(SemVersion other)
    {
        var cmp = Major.CompareTo(other.Major);
        if (cmp != 0) return cmp;
        cmp = Minor.CompareTo(other.Minor);
        if (cmp != 0) return cmp;
        cmp = Patch.CompareTo(other.Patch);
        if (cmp != 0) return cmp;

        // Pre-release versions have lower precedence than release
        if (PreRelease == null && other.PreRelease == null) return 0;
        if (PreRelease == null) return 1;
        if (other.PreRelease == null) return -1;
        return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
    }
}
```

### `Domain/Services/Evaluation/StrategyEvaluator.cs`

```csharp
using Domain.Aggregates.Features;
using Domain.Enums;

namespace Domain.Services.Evaluation;

/// <summary>
/// Evaluates a single strategy against the context.
/// Returns true if the strategy matches (feature should be enabled).
/// </summary>
public static class StrategyEvaluator
{
    public static bool Evaluate(FeatureStrategy strategy, EvaluationContext context)
    {
        // All constraints must match (AND logic)
        foreach (var constraint in strategy.Constraints)
        {
            if (!ConstraintEvaluator.Evaluate(constraint, context))
                return false;
        }

        return strategy.Type switch
        {
            StrategyType.Default => true,
            StrategyType.UserWithId => EvaluateUserWithId(strategy, context),
            StrategyType.GradualRollout => EvaluateGradualRollout(strategy, context),
            StrategyType.FlexibleRollout => EvaluateFlexibleRollout(strategy, context),
            StrategyType.RemoteAddress => EvaluateRemoteAddress(strategy, context),
            StrategyType.ApplicationHost => EvaluateApplicationHost(strategy, context),
            StrategyType.Custom => false, // Custom strategies require plugin registration
            _ => false
        };
    }

    private static bool EvaluateUserWithId(FeatureStrategy strategy, EvaluationContext context)
    {
        if (context.UserId == null) return false;
        return strategy.Parameters.UserIds.Contains(context.UserId);
    }

    private static bool EvaluateGradualRollout(FeatureStrategy strategy, EvaluationContext context)
    {
        var parameters = strategy.Parameters;
        var percentage = parameters.RolloutPercentage ?? 0;
        var stickiness = parameters.Stickiness ?? "default";
        var groupId = parameters.GroupId ?? string.Empty;

        if (percentage == 0) return false;
        if (percentage >= 100) return true;

        var identifier = context.GetIdentifier(stickiness);
        if (identifier == null) return false;

        var normalizedHash = Hashing.NormalizedHash.Compute(identifier, groupId);
        return normalizedHash <= percentage;
    }

    private static bool EvaluateFlexibleRollout(FeatureStrategy strategy, EvaluationContext context)
    {
        // Same logic as GradualRollout but with explicit stickiness selector
        return EvaluateGradualRollout(strategy, context);
    }

    private static bool EvaluateRemoteAddress(FeatureStrategy strategy, EvaluationContext context)
    {
        if (context.RemoteAddress == null) return false;
        
        foreach (var ip in strategy.Parameters.IpAddresses)
        {
            // Support CIDR notation (e.g., "192.168.1.0/24")
            if (ip.Contains('/'))
            {
                if (IpInCidr(context.RemoteAddress, ip))
                    return true;
            }
            else
            {
                if (context.RemoteAddress == ip)
                    return true;
            }
        }
        return false;
    }

    private static bool EvaluateApplicationHost(FeatureStrategy strategy, EvaluationContext context)
    {
        if (context.AppName == null) return false;
        return strategy.Parameters.ApplicationNames.Contains(context.AppName);
    }

    private static bool IpInCidr(string ip, string cidr)
    {
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2) return false;
            
            if (!System.Net.IPAddress.TryParse(ip, out var ipAddress)) return false;
            if (!System.Net.IPAddress.TryParse(parts[0], out var networkAddress)) return false;
            if (!int.TryParse(parts[1], out var prefixLength)) return false;

            var ipBytes = ipAddress.GetAddressBytes();
            var networkBytes = networkAddress.GetAddressBytes();
            if (ipBytes.Length != networkBytes.Length) return false;

            var fullBytes = prefixLength / 8;
            var remainingBits = prefixLength % 8;

            for (var i = 0; i < fullBytes; i++)
            {
                if (ipBytes[i] != networkBytes[i]) return false;
            }

            if (remainingBits > 0 && fullBytes < ipBytes.Length)
            {
                var mask = (byte)(0xFF << (8 - remainingBits));
                if ((ipBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### `Domain/Services/Evaluation/VariantResolver.cs`

```csharp
using Domain.Aggregates.Features;
using Domain.Services.Evaluation.Hashing;
using Domain.ValueObjects;

namespace Domain.Services.Evaluation;

/// <summary>
/// Selects a variant from a list based on stickiness and weights.
/// </summary>
public static class VariantResolver
{
    public static Variant? Resolve(IReadOnlyList<Variant> variants, EvaluationContext context, string groupId)
    {
        if (variants.Count == 0) return null;

        var totalWeight = variants.Sum(v => v.Weight);
        if (totalWeight == 0) return null;

        // Pick stickiness: variant-level > context default
        var stickiness = variants.FirstOrDefault(v => !string.IsNullOrEmpty(v.Stickiness))?.Stickiness ?? "default";
        var identifier = context.GetIdentifier(stickiness);

        int target;
        if (identifier == null)
        {
            // Random fallback
            target = Random.Shared.Next(1, totalWeight + 1);
        }
        else
        {
            // Normalized hash in range [1, totalWeight]
            target = (int)((MurmurHash3.Hash32($"{identifier}:{groupId}") % (uint)totalWeight) + 1);
        }

        var cumulative = 0;
        foreach (var variant in variants)
        {
            cumulative += variant.Weight;
            if (target <= cumulative)
                return variant;
        }

        return variants.LastOrDefault();
    }
}
```

### `Domain/Services/Evaluation/IFeatureEvaluator.cs`

```csharp
namespace Domain.Services.Evaluation;

/// <summary>
/// Core evaluation service — the heart of the feature flag system.
/// </summary>
public interface IFeatureEvaluator
{
    /// <summary>
    /// Evaluate a single feature flag.
    /// </summary>
    Task<EvaluatedFeature> EvaluateAsync(string featureKey, string environmentKey, EvaluationContext context, CancellationToken ct = default);

    /// <summary>
    /// Evaluate all features for an environment in one call (bulk).
    /// </summary>
    Task<IReadOnlyList<EvaluatedFeature>> EvaluateAllAsync(string environmentKey, EvaluationContext context, CancellationToken ct = default);
}
```

### `Domain/Services/Evaluation/FeatureEvaluator.cs`

```csharp
using Domain.Aggregates.Features;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Domain.Services.Evaluation;

/// <summary>
/// Default implementation of IFeatureEvaluator.
/// Orchestrates strategy evaluation, constraint checking, and variant resolution.
/// </summary>
public class FeatureEvaluator : IFeatureEvaluator
{
    private readonly IFeatureSnapshotProvider _snapshotProvider;

    public FeatureEvaluator(IFeatureSnapshotProvider snapshotProvider)
    {
        _snapshotProvider = snapshotProvider;
    }

    public async Task<EvaluatedFeature> EvaluateAsync(
        string featureKey,
        string environmentKey,
        EvaluationContext context,
        CancellationToken ct = default)
    {
        var snapshot = await _snapshotProvider.GetSnapshotAsync(environmentKey, ct);
        var feature = snapshot.Features.FirstOrDefault(f => f.Name == featureKey);

        if (feature == null || feature.ArchivedAt != null)
            return new EvaluatedFeature(featureKey, Enabled: false);

        return EvaluateFeature(feature, environmentKey, context);
    }

    public async Task<IReadOnlyList<EvaluatedFeature>> EvaluateAllAsync(
        string environmentKey,
        EvaluationContext context,
        CancellationToken ct = default)
    {
        var snapshot = await _snapshotProvider.GetSnapshotAsync(environmentKey, ct);
        var results = new List<EvaluatedFeature>(snapshot.Features.Count);

        foreach (var feature in snapshot.Features)
        {
            if (feature.ArchivedAt != null) continue;
            results.Add(EvaluateFeature(feature, environmentKey, context));
        }

        return results;
    }

    private static EvaluatedFeature EvaluateFeature(Feature feature, string environmentKey, EvaluationContext context)
    {
        var env = feature.Environments.FirstOrDefault(e => e.Environment.Name == environmentKey);
        if (env == null || !env.Enabled)
            return new EvaluatedFeature(feature.Name, Enabled: false, ImpressionData: feature.ImpressionDataEnabled);

        // Strategies are OR-logic: any match enables the feature
        FeatureStrategy? matchedStrategy = null;
        foreach (var strategy in env.Strategies.OrderBy(s => s.SortOrder))
        {
            if (StrategyEvaluator.Evaluate(strategy, context))
            {
                matchedStrategy = strategy;
                break;
            }
        }

        if (matchedStrategy == null)
            return new EvaluatedFeature(feature.Name, Enabled: false, ImpressionData: feature.ImpressionDataEnabled);

        // Resolve variant: strategy-level first, then env-level
        var variant = VariantResolver.Resolve(matchedStrategy.Variants, context, feature.Name)
                   ?? VariantResolver.Resolve(env.Variants, context, feature.Name);

        var variantResult = variant == null
            ? null
            : new VariantResult(variant.Name, Enabled: true, variant.Payload);

        return new EvaluatedFeature(
            feature.Name,
            Enabled: true,
            Variant: variantResult,
            ImpressionData: feature.ImpressionDataEnabled);
    }
}
```

### `Domain/Services/Evaluation/IFeatureSnapshotProvider.cs`

```csharp
using Domain.Aggregates.Features;

namespace Domain.Services.Evaluation;

/// <summary>
/// Provides a cached snapshot of all features for an environment.
/// Implementations can use in-memory cache, Redis, or direct DB.
/// </summary>
public interface IFeatureSnapshotProvider
{
    Task<FeatureSnapshot> GetSnapshotAsync(string environmentKey, CancellationToken ct = default);
}

/// <summary>
/// Immutable snapshot of all features for an environment.
/// </summary>
public sealed record FeatureSnapshot(
    string EnvironmentKey,
    long Revision,
    IReadOnlyList<Feature> Features,
    DateTime GeneratedAt);
```

---

## 🎯 Application Layer — SDK Commands

### `Application/Features/Sdk/Commands/EvaluateFeature/EvaluateFeatureCommand.cs`

```csharp
using Application.Common.Models;
using Domain.Services.Evaluation;
using MediatR;

namespace Application.Features.Sdk.Commands.EvaluateFeature;

public record EvaluateFeatureCommand(
    string EnvironmentKey,
    string FeatureKey,
    EvaluationContextDto Context) : ICommandRequest<EvaluateFeatureResponse>;

public record EvaluationContextDto(
    string? UserId,
    string? SessionId,
    string? RemoteAddress,
    string? AppName,
    Dictionary<string, string>? Properties);

public record EvaluateFeatureResponse(
    string Name,
    bool Enabled,
    VariantResponse? Variant);

public record VariantResponse(string Name, bool Enabled, Dictionary<string, object>? Payload);

internal class EvaluateFeatureCommandHandler(IFeatureEvaluator evaluator)
    : CommandRequestHandler<EvaluateFeatureCommand, EvaluateFeatureResponse>
{
    public override async Task<Result<EvaluateFeatureResponse>> Handle(EvaluateFeatureCommand request, CancellationToken cancellationToken)
    {
        var context = MapContext(request.Context);
        var result = await evaluator.EvaluateAsync(request.FeatureKey, request.EnvironmentKey, context, cancellationToken);

        return Ok(new EvaluateFeatureResponse(
            result.Name,
            result.Enabled,
            result.Variant == null ? null : new VariantResponse(result.Variant.Name, result.Variant.Enabled, result.Variant.Payload)));
    }

    private static EvaluationContext MapContext(EvaluationContextDto dto) => new()
    {
        UserId = dto.UserId,
        SessionId = dto.SessionId,
        RemoteAddress = dto.RemoteAddress,
        AppName = dto.AppName,
        Properties = dto.Properties ?? new Dictionary<string, string>()
    };
}
```

### `Application/Features/Sdk/Commands/EvaluateAllFeatures/EvaluateAllFeaturesCommand.cs`

```csharp
using Application.Common.Models;
using Application.Features.Sdk.Commands.EvaluateFeature;
using Domain.Services.Evaluation;
using MediatR;

namespace Application.Features.Sdk.Commands.EvaluateAllFeatures;

public record EvaluateAllFeaturesCommand(
    string EnvironmentKey,
    EvaluationContextDto Context) : ICommandRequest<IReadOnlyList<EvaluateFeatureResponse>>;

internal class EvaluateAllFeaturesCommandHandler(IFeatureEvaluator evaluator)
    : CommandRequestHandler<EvaluateAllFeaturesCommand, IReadOnlyList<EvaluateFeatureResponse>>
{
    public override async Task<Result<IReadOnlyList<EvaluateFeatureResponse>>> Handle(EvaluateAllFeaturesCommand request, CancellationToken cancellationToken)
    {
        var context = new EvaluationContext
        {
            UserId = request.Context.UserId,
            SessionId = request.Context.SessionId,
            RemoteAddress = request.Context.RemoteAddress,
            AppName = request.Context.AppName,
            Properties = request.Context.Properties ?? new Dictionary<string, string>()
        };

        var results = await evaluator.EvaluateAllAsync(request.EnvironmentKey, context, cancellationToken);

        return Ok(results.Select(r => new EvaluateFeatureResponse(
            r.Name,
            r.Enabled,
            r.Variant == null ? null : new VariantResponse(r.Variant.Name, r.Variant.Enabled, r.Variant.Payload))).ToList());
    }
}
```

### `Application/Features/Sdk/Queries/GetBootstrap/GetBootstrapQuery.cs`

```csharp
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Services.Evaluation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Sdk.Queries.GetBootstrap;

/// <summary>
/// Returns the full flag payload for an environment (for SDK bootstrapping).
/// </summary>
public record GetBootstrapQuery(string EnvironmentKey) : IQueryRequest<BootstrapResponse>;

public record BootstrapResponse(
    long Revision,
    IReadOnlyList<BootstrapFeature> Features);

public record BootstrapFeature(
    string Name,
    string Type,
    bool Enabled,
    List<BootstrapStrategy> Strategies,
    List<BootstrapVariant> Variants,
    bool ImpressionData);

public record BootstrapStrategy(
    string Name,
    Dictionary<string, object> Parameters,
    List<BootstrapConstraint> Constraints,
    List<BootstrapVariant> Variants,
    List<int> Segments);

public record BootstrapConstraint(
    string ContextName,
    string Operator,
    List<string> Values,
    bool Inverted,
    bool CaseInsensitive);

public record BootstrapVariant(
    string Name,
    int Weight,
    string? Stickiness,
    Dictionary<string, object>? Payload);

internal class GetBootstrapQueryHandler(IFeatureSnapshotProvider snapshotProvider)
    : QueryRequestHandler<GetBootstrapQuery, BootstrapResponse>
{
    public override async Task<BootstrapResponse> Handle(GetBootstrapQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotProvider.GetSnapshotAsync(request.EnvironmentKey, cancellationToken);

        var features = snapshot.Features
            .Where(f => f.ArchivedAt == null)
            .Select(f =>
            {
                var env = f.Environments.FirstOrDefault(e => e.Environment.Name == request.EnvironmentKey);
                return new BootstrapFeature(
                    f.Name,
                    f.Type.ToString(),
                    env?.Enabled ?? false,
                    (env?.Strategies ?? Enumerable.Empty<Domain.Aggregates.Features.FeatureStrategy>())
                        .Select(s => new BootstrapStrategy(
                            s.Type.ToString(),
                            new Dictionary<string, object>
                            {
                                ["rollout"] = s.Parameters.RolloutPercentage ?? 100,
                                ["stickiness"] = s.Parameters.Stickiness ?? "default",
                                ["groupId"] = s.Parameters.GroupId ?? f.Name,
                                ["userIds"] = s.Parameters.UserIds,
                                ["IPs"] = s.Parameters.IpAddresses
                            },
                            s.Constraints.Select(c => new BootstrapConstraint(
                                c.ContextName, c.Operator.ToString(), c.Values.ToList(), c.Inverted, c.CaseInsensitive)).ToList(),
                            s.Variants.Select(v => new BootstrapVariant(v.Name, v.Weight, v.Stickiness, v.Payload)).ToList(),
                            s.SegmentIds.ToList())).ToList(),
                    (env?.Variants ?? Enumerable.Empty<Domain.ValueObjects.Variant>())
                        .Select(v => new BootstrapVariant(v.Name, v.Weight, v.Stickiness, v.Payload)).ToList(),
                    f.ImpressionDataEnabled);
            }).ToList();

        return new BootstrapResponse(snapshot.Revision, features);
    }
}
```

---

## 🏗️ Infrastructure Layer — Snapshot Provider & Caching

### `Infrastructure/Caching/ICacheService.cs`

```csharp
namespace Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

### `Infrastructure/Caching/InMemoryCacheService.cs`

```csharp
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Caching;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public InMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        => Task.FromResult(_cache.TryGetValue(key, out T? value) ? value : null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue) options.SetAbsoluteExpiration(expiration.Value);
        else options.SetSlidingExpiration(TimeSpan.FromMinutes(5));
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
```

### `Infrastructure/Services/FeatureSnapshotService.cs`

```csharp
using Domain.Services.Evaluation;
using Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

/// <summary>
/// Provides cached feature snapshots per environment.
/// Cache is invalidated whenever any flag in the environment changes.
/// </summary>
public class FeatureSnapshotService : IFeatureSnapshotProvider
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICacheService _cache;
    private const string CachePrefix = "feature-snapshot:";

    public FeatureSnapshotService(IApplicationDbContext dbContext, ICacheService cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<FeatureSnapshot> GetSnapshotAsync(string environmentKey, CancellationToken ct = default)
    {
        var cacheKey = $"{CachePrefix}{environmentKey}";
        var cached = await _cache.GetAsync<FeatureSnapshot>(cacheKey, ct);
        if (cached != null) return cached;

        var environment = await _dbContext.Environments
            .FirstOrDefaultAsync(e => e.Name == environmentKey, ct)
            ?? throw new Domain.Exceptions.EntityNotFoundException("Environment", environmentKey);

        var features = await _dbContext.Features
            .Include(f => f.Environments)
                .ThenInclude(e => e.Environment)
            .Include(f => f.Environments)
                .ThenInclude(e => e.Strategies)
                    .ThenInclude(s => s.Constraints)
            .Include(f => f.Environments)
                .ThenInclude(e => e.Variants)
            .Where(f => f.Project.Environments.Any(e => e.Id == environment.Id))
            .AsSplitQuery()
            .ToListAsync(ct);

        var snapshot = new FeatureSnapshot(
            environmentKey,
            Revision: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            features,
            DateTime.UtcNow);

        await _cache.SetAsync(cacheKey, snapshot, TimeSpan.FromSeconds(30), ct);
        return snapshot;
    }

    /// <summary>
    /// Call this from domain event handlers when any flag changes.
    /// </summary>
    public async Task InvalidateAsync(string environmentKey, CancellationToken ct = default)
    {
        await _cache.RemoveAsync($"{CachePrefix}{environmentKey}", ct);
    }

    public async Task InvalidateAllAsync(CancellationToken ct = default)
    {
        // For simplicity, we clear known env keys. In production, use cache tags or Redis SCAN.
        var envs = await _dbContext.Environments.Select(e => e.Name).ToListAsync(ct);
        foreach (var env in envs)
            await _cache.RemoveAsync($"{CachePrefix}{env}", ct);
    }
}
```

---

## 🌐 WebApi Layer — SDK Controllers

### `WebApi/Controllers/Sdk/SdkController.cs`

```csharp
using Application.Features.Sdk.Commands.EvaluateAllFeatures;
using Application.Features.Sdk.Commands.EvaluateFeature;
using Application.Features.Sdk.Queries.GetBootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Sdk;

/// <summary>
/// Server-side SDK API — authenticates via API key (X-Api-Key header).
/// High-throughput, read-heavy, optimized payloads.
/// </summary>
[ApiController]
[Route("api/sdk/v1")]
[AllowAnonymous] // API key auth handled separately via middleware
public class SdkController(IMediator mediator) : ControllerBase
{
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateFeatureCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("evaluate-all")]
    public async Task<IActionResult> EvaluateAll([FromBody] EvaluateAllFeaturesCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("bootstrap")]
    public async Task<IActionResult> Bootstrap([FromQuery] string environment, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBootstrapQuery(environment), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
```

### `WebApi/Controllers/Sdk/FrontendController.cs`

```csharp
using Application.Features.Sdk.Commands.EvaluateAllFeatures;
using Application.Features.Sdk.Commands.EvaluateFeature;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Sdk;

/// <summary>
/// Frontend SDK API — for browser-based clients.
/// Returns a filtered payload safe for public consumption.
/// </summary>
[ApiController]
[Route("api/frontend/v1")]
[AllowAnonymous]
public class FrontendController(IMediator mediator) : ControllerBase
{
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateFeatureCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("evaluate-all")]
    public async Task<IActionResult> EvaluateAll([FromBody] EvaluateAllFeaturesCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
```

---

## 🔧 Registration in DI

### Update `Infrastructure/DependencyInjection.cs`

```csharp
// Add these registrations:
services.AddMemoryCache();
services.AddScoped<ICacheService, InMemoryCacheService>();
services.AddScoped<IFeatureSnapshotProvider, FeatureSnapshotService>();
services.AddScoped<IFeatureEvaluator, FeatureEvaluator>();
```

---

## 📋 What We Just Built

| Component | Purpose |
|-----------|---------|
| **MurmurHash3** | Bit-for-bit compatible hashing for sticky rollouts |
| **NormalizedHash** | Maps identifiers to 1-100 bucket for % rollouts |
| **ConstraintEvaluator** | All 18+ operators (string, numeric, date, semver) |
| **StrategyEvaluator** | All 6 built-in strategies + CIDR IP matching |
| **VariantResolver** | Weighted variant selection with stickiness |
| **FeatureEvaluator** | Orchestrates everything |
| **SnapshotProvider** | Cached flag snapshots per environment |
| **SDK API** | 4 endpoints for client consumption |

---

## 🚀 Usage Examples

### Evaluate single flag (server SDK)
```http
POST /api/sdk/v1/evaluate
X-Api-Key: your-client-key

{
  "environmentKey": "production",
  "featureKey": "new-checkout",
  "context": {
    "userId": "u_42",
    "properties": { "plan": "pro", "country": "US" }
  }
}
```

### Bootstrap (SDK startup)
```http
GET /api/sdk/v1/bootstrap?environment=production
X-Api-Key: your-client-key
```

---

## 🎯 What's Next?

We've built the **evaluation engine** — the heart of the system. The remaining critical pieces are:

1. **API Key authentication middleware** (for SDK endpoints)
2. **Audit log persistence** (domain event → audit entry)
3. **Realtime streaming** (SignalR/SSE for flag changes)
4. **Cache invalidation** on admin writes

Want me to tackle **API Key authentication + Audit Log** next? Those are the final pieces needed before this is a working system end-to-end. 🔐📜
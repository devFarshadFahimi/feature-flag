using Application.Features.Features.Queries.GetFeatureById;
using Domain.ValueObjects;

namespace Application.Common.Mappers;


public static class FeatureMappingConfig
{
    public static void Configure()
    {
        // StrategyParameters → Dictionary<string, object>
        _ = TypeAdapterConfig<StrategyParameters, Dictionary<string, object>>
            .NewConfig()
            .MapWith(p => ConvertToDictionary(p));

        // Constraint → ConstraintResponse
        _ = TypeAdapterConfig<Constraint, ConstraintResponse>
            .NewConfig()
            .Map(dest => dest.Operator, src => src.Operator.ToString())
            .Map(dest => dest.Values, src => src.Values.ToList());

        // FeatureStrategy → FeatureStrategyResponse
        _ = TypeAdapterConfig<FeatureStrategy, FeatureStrategyResponse>
            .NewConfig()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.Parameters, src => src.Parameters.Adapt<Dictionary<string, object>>())
            .Map(dest => dest.Constraints, src => src.Constraints.Adapt<List<ConstraintResponse>>())
            .Map(dest => dest.SegmentIds, src => src.SegmentIds.ToList());

        // FeatureEnvironment → FeatureEnvironmentResponse
        _ = TypeAdapterConfig<FeatureEnvironment, FeatureEnvironmentResponse>
            .NewConfig()
            .Map(dest => dest.Strategies, src => src.Strategies.Adapt<List<FeatureStrategyResponse>>());

        // Feature → FeatureResponse
        _ = TypeAdapterConfig<Feature, FeatureResponse>
            .NewConfig()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.Lifecycle, src => src.LifeCycle.ToString())
            .Map(dest => dest.Tags, src => src.Tags.ToList())
            .Map(dest => dest.Environments, src => src.Environments.Adapt<List<FeatureEnvironmentResponse>>());
    }

    private static Dictionary<string, object> ConvertToDictionary(StrategyParameters src)
    {
        return new Dictionary<string, object>
        {
            ["rolloutPercentage"] = src.RolloutPercentage ?? 0,
            ["stickiness"] = src.Stickiness ?? "default",
            ["groupId"] = src.GroupId ?? string.Empty,
            ["userIds"] = src.UserIds,
            ["ipAddresses"] = src.IpAddresses,
            ["applicationNames"] = src.ApplicationNames,
            ["customParameters"] = src.CustomParameters
        };
    }
}
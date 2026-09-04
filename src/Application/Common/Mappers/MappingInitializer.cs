namespace Application.Common.Mappers;

/// <summary>
/// Call once at application startup to register all Mapster configurations.
/// </summary>
public static class MappingInitializer
{
    public static void Initialize()
    {
        FeatureMappingConfig.Configure();
        EnvironmentMappingConfig.Configure();
        SegmentMappingConfig.Configure();
        ChangeRequestMappingConfig.Configure();
        UserMappingConfig.Configure();
        ProjectMappingConfig.Configure();
    }
}
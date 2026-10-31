namespace Application.Common.Mappers;

public static class EnvironmentMappingConfig
{
    public static void Configure()
    {
        // Environment → EnvironmentResponse
        _ = TypeAdapterConfig<Environment, EnvironmentResponse>
            .NewConfig()
            .Map(dest => dest.Type, src => src.Type.ToString());
    }
}

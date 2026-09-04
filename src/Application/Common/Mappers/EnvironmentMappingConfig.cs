namespace Application.Common.Mappers;

public static class EnvironmentMappingConfig
{
    public static void Configure()
    {
        // ApiToken → ApiTokenResponse
        _ = TypeAdapterConfig<ApiToken, ApiTokenResponse>
            .NewConfig()
            .Map(dest => dest.TokenType, src => src.TokenType.ToString());

        // Environment → EnvironmentResponse
        _ = TypeAdapterConfig<Environment, EnvironmentResponse>
            .NewConfig()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.Tokens, src => src.Tokens.Adapt<List<ApiTokenResponse>>());
    }
}

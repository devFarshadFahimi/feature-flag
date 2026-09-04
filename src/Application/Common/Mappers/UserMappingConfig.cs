using Application.Features.Auth.Commands.Login;

namespace Application.Common.Mappers;

public static class UserMappingConfig
{
    public static void Configure()
    {
        // User → UserResponse
        _ = TypeAdapterConfig<User, UserResponse>
            .NewConfig()
            .Map(dest => dest.Role, src => src.Role.ToString());
    }
}

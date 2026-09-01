using Microsoft.Extensions.Configuration;

namespace Infrastructure.Options.Extensions;

public class AuthSettingOptionsSetup(IConfiguration configuration)
    : BaseOptionsSetup<AuthTokenOption>(configuration, "AuthSettings")
{
}

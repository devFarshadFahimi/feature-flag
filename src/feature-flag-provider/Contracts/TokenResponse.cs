using FeatureFlag.Provider.Contracts;

namespace FeatureFlag.Provider.ResponseModels;

public sealed record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiration,
    DateTimeOffset RefreshTokenExpiration,
    AuthenticatedUser User);

namespace Application.Common.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Gets or sets retired keys accepted during a bounded deployment overlap; new tokens use SecretKey.</summary>
    public string[] PreviousSigningKeys { get; set; } = [];

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 60;

    public int RefreshTokenExpirationDays { get; set; } = 7;
}
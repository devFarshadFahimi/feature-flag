namespace Infrastructure.Options;

public class AuthTokenOption
{
    public bool IsEnabled { get; set; }
    public string SigningKey { get; set; } = null!;
    public bool ValidateIssuer { get; set; }
    public bool ValidateAudience { get; set; }
    public bool ValidateLifetime { get; set; }
    public bool ValidateIssuerSigningKey { get; set; }
    public string ValidIssuer { get; set; } = null!;
    public string ValidAudience { get; set; } = null!;
    public long TokenExpireMinutes { get; set; }
}
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace WebApi.Configuration;

public static class TransportSecurityConfiguration
{
    /// <summary>Trusts only explicitly configured proxy addresses and requires a shared certificate for production key protection.</summary>
    /// <param name = "builder">The application builder containing trusted proxy and certificate configuration.</param>
    public static void AddTransportSecurity(this WebApplicationBuilder builder)
    {
        var proxies = builder.Configuration
            .GetSection("Security:TrustedProxies")
            .Get<string[]>() ?? [];

        if (proxies.Any(proxy => !IPAddress.TryParse(proxy, out _)))
        {
            throw new InvalidOperationException("نشانی کارگزار مورد اعتماد معتبر نیست.");
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            // An empty trust list must disable processing, never trust every sender.
            if (proxies.Length == 0)
            {
                options.ForwardedHeaders = ForwardedHeaders.None;
            }

            foreach (var proxy in proxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });
        builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(180));

        var protection = builder.Services
            .AddDataProtection()
            .SetApplicationName("feature-flag");
        var certificatePath = builder.Configuration["Security:DataProtection:CertificatePath"];

        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            if (!builder.Environment.IsDevelopment())
            {
                throw new InvalidOperationException("گواهی حفاظت از کلیدهای ذخیره‌شده برای محیط عملیاتی الزامی است.");
            }

            return;
        }

        try
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, builder.Configuration["Security:DataProtection:CertificatePassword"], X509KeyStorageFlags.EphemeralKeySet);
            using var signingKey = certificate.GetRSAPrivateKey();

            if (signingKey is null || signingKey.KeySize < 2048 || certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow || certificate.NotBefore.ToUniversalTime() > DateTime.UtcNow)
            {
                certificate.Dispose();

                throw new InvalidOperationException("گواهی حفاظت از کلیدها باید معتبر و دارای کلید خصوصی باشد.");
            }

            builder.Services.AddSingleton(certificate);
            protection.ProtectKeysWithCertificate(certificate);

            var previousPaths = builder.Configuration
                .GetSection("Security:DataProtection:PreviousCertificatePaths")
                .Get<string[]>() ?? [];

            if (previousPaths.Length > 3 || previousPaths.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException("تعداد گواهی‌های پیشین بیش از حد مجاز است.");
            }

            var previousCertificates = previousPaths
                .Select(path => X509CertificateLoader.LoadPkcs12FromFile(path, builder.Configuration["Security:DataProtection:CertificatePassword"], X509KeyStorageFlags.EphemeralKeySet))
                .ToArray();

            if (previousCertificates.Any(previous => !previous.HasPrivateKey))
            {
                throw new InvalidOperationException("گواهی پیشین باید دارای کلید خصوصی باشد.");
            }

            protection.UnprotectKeysWithAnyCertificate(previousCertificates
                .Prepend(certificate)
                .ToArray());
            foreach (var previous in previousCertificates)
            {
                builder.Services.AddSingleton(previous);
            }
        }
        catch (Exception exception) when (exception is System.Security.Cryptography.CryptographicException or IOException)
        {
            throw new InvalidOperationException("بارگذاری گواهی حفاظت از کلیدها ناموفق بود.", exception);
        }
    }
}
using System.Text;
using Application.Common.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Application.Common.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Configuration;

public static class JwtConfiguration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>() ?? throw new InvalidOperationException("تنظیمات احراز هویت پیکربندی نشده است.");

        services
            .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (!Guid.TryParse(context.Principal?.FindFirstValue("sub"), out var id))
                    {
                        context.Fail("هویت کاربر معتبر نیست.");

                        return;
                    }


                    var db = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
                    var user = await db.Users
                        .AsNoTracking()
                        .SingleOrDefaultAsync(u => u.Id == id, context.HttpContext.RequestAborted);

                    if (user is null || !user.IsActive || context.Principal?.FindFirstValue("security_version") != user.SecurityVersion.ToString())
                    {
                        context.Fail("نشست کاربر منقضی یا لغو شده است.");
                    }
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 401, Title = "احراز هویت معتبر الزامی است." });
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 403, Title = "اجازه دسترسی ندارید." });
                },
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }


                    return Task.CompletedTask;
                }
            };
        });
        services
            .AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "RootAdmin"))
            .AddPolicy("EditorOrAbove", policy => policy.RequireRole("Editor", "Admin", "RootAdmin"));

        return services;
    }
}
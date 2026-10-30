using Application.Common.Services;
using BusinessMakerFramework.Endpoint.WebApi.ApiDescriptorSetup;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace WebApi.ProgramConfiguration;

public static class ApplicationBuilder
{
    public static async Task UseWebApiConfiguration(this WebApplication app)
    {
        // Seed database
        try
        {
            await app.Services.SeedDatabaseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        _ = app.UseExceptionHandler();
        app.MapOpenApiAndScalar();
        _ = app.UseHttpsRedirection();
        _ = app.UseCors(p =>
        {
            _ = p.AllowAnyHeader();
            _ = p.AllowAnyMethod();
            _ = p.AllowAnyOrigin();

            //var origins = app.Configuration
            //    .GetSection("Cors:AllowedOrigins")
            //    .Get<string[]>() ?? [];

            //if (origins.Length > 0)
            //{
            //    _ = p.WithOrigins(origins);
            //}
        });
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        app.UseHealthCheckEndpoints();
        _ = app
            .MapGet("/health/live", () => Results.Ok(new { status = "alive" }))
            .AllowAnonymous();
        _ = app
            .MapGet("/health/ready", ReadinessAsync)
            .AllowAnonymous();
        _ = app.MapControllers();
        _ = app.MapApiDescriptor();
        app.Run();
    }

    /// <summary>Reports database connectivity and migration readiness without seeding, migrating or exposing connection details.</summary>
    private static async Task<IResult> ReadinessAsync(IApplicationDbContext db, HttpContext context, ILoggerFactory logs, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            if (await db.Database.CanConnectAsync(ct) && !(await db.Database.GetPendingMigrationsAsync(ct)).Any())
            {
                return Results.Ok(new
                {
                    status = "ready"
                });
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logs
                .CreateLogger(nameof(ApplicationBuilder))
                .LogWarning(exception, "بررسی آمادگی پایگاه داده ناموفق بود.");
        }

        return Results.Json(new
        {
            status = "unavailable",
            message = "پایگاه داده برای پاسخ‌گویی آماده نیست."
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
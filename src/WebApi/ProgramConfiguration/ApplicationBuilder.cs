using BusinessMakerFramework.Endpoint.WebApi.ApiDescriptorSetup;
using Infrastructure;

namespace WebApi.ProgramConfiguration;

public static class ApplicationBuilder
{
    public static async Task UseWebApiConfiguration(this WebApplication app)
    {
        // Seed database
        await app.Services.SeedDatabaseAsync();
        app.UseExceptionHandler();
        app.MapOpenApiAndScalar();
        _ = app.UseHttpsRedirection();
        _ = app.UseCors(p =>
        {
            _ = p.AllowAnyHeader();
            _ = p.AllowAnyMethod();

            var origins = app.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            if (origins.Length > 0)
            {
                _ = p.WithOrigins(origins);
            }
        });
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        app.UseHealthCheckEndpoints();
        _ = app.MapControllers();
        _ = app.MapApiDescriptor();
        app.Run();
    }
}
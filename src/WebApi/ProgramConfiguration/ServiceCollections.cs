using Application;
using BusinessMakerFramework.Endpoint.WebApi.ApiDescriptorSetup;
using BusinessMakerFramework.Endpoint.WebApi.HateoasSetup;
using BusinessMakerFramework.Endpoint.WebApi.ScalarSetup.Transformers;
using BusinessMakerFramework.Endpoint.WebApi.SwaggerSetup;
using EdgeServicesBuildingBlock.Shared.ExceptionHandlers;
using Infrastructure;

namespace WebApi.ProgramConfiguration;

public static class ServiceCollections
{
    public static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHateoasGeneratorService();
        _ = SetupOpenApi(builder);

        _ = builder.Services.AddHealthChecks();

        builder.Services.AddFrameworkExceptionHandler();
        _ = builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        _ = builder.Services.AddControllers();
        _ = builder.Services.AddFrameworkApiVersioning();
        _ = builder.Services.AddInfrastructureServices(builder.Configuration);
        _ = builder.Services.AddApplicationServices();
        _ = builder.Services
            .AddHttpClient()
            .AddHttpContextAccessor();


        //_ = builder.Services.AddFrameworkHealthCheck()
        //    //.AddDbContextCheck<ApplicationDbContext>()
        //    .AddRedis(builder.Configuration.GetValue<string>("Redis:ConnectionString")!);

        //_ = builder.Services
        //    .AddFrameworkHealthCheckUI(
        //        builder.Configuration.GetValue<string>("ServiceRegistration:ServiceHealthCheckEndpoint")!)
        //    .AddPostgreSqlStorage(builder.Configuration.GetConnectionString("HealthCheckConnection")!);

        builder.AddAspireOrchestrationStandaloneOtelMonitoring("payment", true,
           meter => meter.AddMeter("MyCompany.MyProduct.MyLibrary"),
           trace => trace.AddSource("MyCompany.MyService"));
        //.SetResourceBuilder(
        //    ResourceBuilder.CreateDefault()
        //        .AddService("MyService")
        //        .AddAttributes(new Dictionary<string, object>
        //        {
        //            ["deployment.environment"] = builder.Environment.EnvironmentName
        //        })
        //)


        // Add layers
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddJwtAuthentication(builder.Configuration);
        return builder;
    }


    private static WebApplicationBuilder SetupOpenApi(WebApplicationBuilder builder)
    {
        _ = builder.Services.AddApiDescriptor();
        List<OpenApiInfoOptions> newOpenApiInfoOptions = builder.GetOpenApiInfoOptions();

        foreach (var description in newOpenApiInfoOptions)
        {
            builder.AddOpenApiDocument(
                description.OpenApiInfo?.Title ?? "Invalid Service Title",
                description.OpenApiInfo?.Description ?? "Invalid Service Description",
                version: description.Version ?? "v1",
                action: p =>
                {
                    _ = p.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                    _ = p.AddOperationTransformer<ActionSummaryNamingTransformer>();
                });
        }
        return builder;
    }

    private static List<OpenApiInfoOptions> GetOpenApiInfoOptions(this WebApplicationBuilder builder)
    {
        var newOpenApiInfoOptions = new List<OpenApiInfoOptions>();
        builder.Configuration.GetSection("OpenApiInfoOptions").Bind(newOpenApiInfoOptions);
        return newOpenApiInfoOptions;
    }

}

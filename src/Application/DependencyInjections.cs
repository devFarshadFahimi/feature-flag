using System.Reflection;
using BusinessMakerFramework.Application.Common.Extensions;
using BusinessMakerFramework.Shared.Extensions.Caching.InMemory.Extensions.DependencyInjection;
using BusinessMakerFramework.Shared.Extensions.Serializer.Microsoft.DependencyInjections;
using BusinessMakerFramework.SourceGenerator;
using BusinessMakerFramework.SourceGenerator.Shared.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjections
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        _ = services.AddValidationCultureInfo();
        _ = services.AddFrameworkMediator();
        _ = services.AddCommandHandlers();
        _ = services.AddNotificationHandlers();

        _ = services.AddFrameworkNotificationPublisher();
        _ = services.AddFrameworkPerformanceBehaviour();
        _ = services.AddFrameworkValidationBehaviour();
        _ = services.AddFrameworkUnhandledExceptionBehaviour();
        _ = services.AddBuildingBlockMicrosoftSerializer();
        _ = services.AddBuildingBlockInMemoryCache();
        _ = TypeAdapterConfig.GlobalSettings.Default.MapToConstructor(true);
        _ = TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

        return services;
    }
}

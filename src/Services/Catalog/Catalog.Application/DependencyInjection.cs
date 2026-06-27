using BuildingBlocks.Application.DependencyInjection;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddApplicationMessaging(AssemblyReference.Assembly);

        // Apply Mapster IRegister mappings to the global config used by Adapt<>().
        TypeAdapterConfig.GlobalSettings.Scan(AssemblyReference.Assembly);

        return services;
    }
}

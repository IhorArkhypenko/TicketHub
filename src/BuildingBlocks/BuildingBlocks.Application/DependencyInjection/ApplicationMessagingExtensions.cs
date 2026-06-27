using System.Reflection;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application.DependencyInjection;

public static class ApplicationMessagingExtensions
{
    /// <summary>
    /// Registers the lightweight CQRS sender, the validation/logging pipeline behaviors,
    /// and scans the given assemblies for request handlers and FluentValidation validators.
    /// </summary>
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be provided to scan for handlers.", nameof(assemblies));
        }

        services.AddScoped<ISender, Sender>();

        services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);

        // Behaviors run in registration order; logging wraps validation wraps the handler.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

        RegisterHandlers(services, assemblies);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var handlerInterface = typeof(IRequestHandler<,>);

        IEnumerable<Type> implementations = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false });

        foreach (Type implementation in implementations)
        {
            IEnumerable<Type> services2 = implementation.GetInterfaces()
                .Where(@interface => @interface.IsGenericType
                    && @interface.GetGenericTypeDefinition() == handlerInterface);

            foreach (Type service in services2)
            {
                services.AddTransient(service, implementation);
            }
        }
    }
}

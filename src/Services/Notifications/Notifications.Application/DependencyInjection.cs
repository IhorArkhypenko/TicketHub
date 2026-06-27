using System.Reflection;
using BuildingBlocks.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Notifications.Application;

public static class DependencyInjection
{
    public static readonly Assembly Assembly = typeof(DependencyInjection).Assembly;

    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddApplicationMessaging(Assembly);
        return services;
    }
}

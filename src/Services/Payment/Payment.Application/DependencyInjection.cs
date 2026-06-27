using System.Reflection;
using BuildingBlocks.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.Application;

public static class DependencyInjection
{
    public static readonly Assembly Assembly = typeof(DependencyInjection).Assembly;

    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        services.AddApplicationMessaging(Assembly);
        return services;
    }
}

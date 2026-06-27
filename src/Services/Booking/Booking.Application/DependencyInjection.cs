using System.Reflection;
using BuildingBlocks.Application.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Application;

public static class DependencyInjection
{
    public static readonly Assembly Assembly = typeof(DependencyInjection).Assembly;

    public static IServiceCollection AddBookingApplication(this IServiceCollection services)
    {
        services.AddApplicationMessaging(Assembly);
        return services;
    }
}

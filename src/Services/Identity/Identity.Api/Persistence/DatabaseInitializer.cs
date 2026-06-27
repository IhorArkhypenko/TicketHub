using Duende.IdentityServer.EntityFramework.DbContexts;
using Identity.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Persistence;

/// <summary>
/// Applies migrations for the Identity and Duende operational stores and seeds a demo user,
/// on host startup. Implemented as a hosted service so EF design-time tooling (which builds
/// the host but never starts it) does not trigger database work while scaffolding migrations.
/// </summary>
internal sealed class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _services;

    public DatabaseInitializer(IServiceProvider services) => _services = services;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        await services.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync(cancellationToken);

        ApplicationDbContext appDbContext = services.GetRequiredService<ApplicationDbContext>();
        await appDbContext.Database.MigrateAsync(cancellationToken);

        await SeedDemoUserAsync(services);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task SeedDemoUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        const string email = "alice@tickethub.local";

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Alice"
        };

        await userManager.CreateAsync(user, "Pass123$");
    }
}

using Identity.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.Persistence;

/// <summary>EF Core store for ASP.NET Core Identity users in the `identity` schema.</summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public const string Schema = "identity";

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);
        base.OnModelCreating(builder);
    }
}

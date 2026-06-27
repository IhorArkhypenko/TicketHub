using Microsoft.AspNetCore.Identity;

namespace Identity.Api.Models;

/// <summary>Application user. Duende issues tokens for these via ASP.NET Core Identity.</summary>
public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Observability.Security;

public static class JwtAuthenticationExtensions
{
    public const string ScopePolicy = "RequireApiScope";

    /// <summary>
    /// Configures JWT bearer validation against the Identity provider (issuer, audience,
    /// lifetime, signature). Adds a policy requiring the given API scope. Shared by every
    /// resource service and by the gateway.
    /// </summary>
    public static IServiceCollection AddTicketHubJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        string audience,
        string scope)
    {
        string authority = configuration["Identity:Authority"] ?? "http://localhost:5102";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false; // dev: Identity is served over http
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = audience
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(ScopePolicy, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("scope", scope));

        return services;
    }
}

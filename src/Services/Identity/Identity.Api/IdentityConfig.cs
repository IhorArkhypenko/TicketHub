using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Identity.Api;

/// <summary>
/// In-memory IdentityServer configuration: identity resources, API scope/resource and clients.
/// Operational data (grants, signing keys) is persisted in PostgreSQL; clients are kept in code
/// for a learning project.
/// </summary>
public static class IdentityConfig
{
    public const string CatalogApiScope = "catalog.api";
    public const string CatalogApiResource = "catalog";
    public const string BookingApiScope = "booking.api";
    public const string BookingApiResource = "booking";

    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope(CatalogApiScope, "Catalog API"),
        new ApiScope(BookingApiScope, "Booking API")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource(CatalogApiResource, "Catalog API")
        {
            Scopes = { CatalogApiScope }
        },
        new ApiResource(BookingApiResource, "Booking API")
        {
            Scopes = { BookingApiScope }
        }
    ];

    public static IEnumerable<Client> Clients =>
    [
        // Public client (SPA / native) using Authorization Code + PKCE — the modern, secure
        // flow for public clients (no client secret).
        new Client
        {
            ClientId = "tickethub-spa",
            ClientName = "TicketHub SPA",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = true,
            RequireConsent = false,
            AllowOfflineAccess = true,
            RedirectUris =
            {
                "http://localhost:8080/swagger/oauth2-redirect.html",
                "https://oauth.pstmn.io/v1/callback"
            },
            PostLogoutRedirectUris = { "http://localhost:8080" },
            AllowedCorsOrigins = { "http://localhost:8080" },
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                CatalogApiScope,
                BookingApiScope
            }
        },

        // Machine-to-machine client (client credentials) — used for automated checks of the
        // protected API without an interactive login.
        new Client
        {
            ClientId = "tickethub-machine",
            ClientName = "TicketHub Machine",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedScopes = { CatalogApiScope, BookingApiScope }
        }
    ];
}

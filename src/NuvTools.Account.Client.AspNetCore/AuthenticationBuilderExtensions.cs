using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace NuvTools.Account.Client.AspNetCore;

/// <summary>Registers the scheme that accepts Nuv Tools Account access tokens.</summary>
public static class AuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds a bearer scheme that accepts an access token Nuv Tools Account issued for this product:
    /// signed with a key from the Account's discovery document, issued by exactly
    /// <see cref="NuvToolsAccountOptions.Authority"/>, for exactly
    /// <see cref="NuvToolsAccountOptions.Audience"/>, and not expired.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The claims keep the Account's names (<see cref="NuvToolsAccountClaims"/>): inbound mapping is
    /// off, so <c>sub</c> stays <c>sub</c>.
    /// </para>
    /// <para>
    /// It does not become the default scheme. A product keeps its own token for everything else and
    /// names this scheme on the endpoint that turns the Account's token into its own session —
    /// <c>[Authorize(AuthenticationSchemes = NuvToolsAccountDefaults.AuthenticationScheme)]</c>.
    /// </para>
    /// </remarks>
    public static AuthenticationBuilder AddNuvToolsAccount(
        this AuthenticationBuilder builder,
        Action<NuvToolsAccountOptions> configure,
        string authenticationScheme = NuvToolsAccountDefaults.AuthenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new NuvToolsAccountOptions();
        configure(options);

        return builder.AddJwtBearer(authenticationScheme, bearer => Configure(bearer, options));
    }

    private static void Configure(JwtBearerOptions bearer, NuvToolsAccountOptions options)
    {
        bearer.MapInboundClaims = false;

        if (!options.IsConfigured)
        {
            // No metadata address, so nothing is fetched and nothing can be resolved later: an
            // empty configuration is a permanent absence of signing keys, and every token fails.
            bearer.Configuration = new OpenIdConnectConfiguration();
            bearer.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true };

            return;
        }

        bearer.Authority = options.Authority;

        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Authority,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = options.ClockSkew,
            NameClaimType = NuvToolsAccountClaims.Name
        };
    }
}

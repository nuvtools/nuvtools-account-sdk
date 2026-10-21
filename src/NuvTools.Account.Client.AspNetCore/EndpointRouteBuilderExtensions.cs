using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NuvTools.Account.Client.AspNetCore;

/// <summary>Serves the Account's address to a browser client.</summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Answers <paramref name="path"/> with the configured <see cref="NuvToolsAccountClientSettings"/>
    /// as JSON (<c>authority</c>, <c>clientId</c>), uncached and open to anybody — neither value is a
    /// secret. <c>NuvTools.Account.Client.Blazor</c> reads it from the same path by default.
    /// </summary>
    /// <remarks>
    /// Register the settings first, for example
    /// <c>services.Configure&lt;NuvToolsAccountClientSettings&gt;(configuration.GetSection("Account"))</c>.
    /// </remarks>
    public static IEndpointConventionBuilder MapNuvToolsAccountSettings(
        this IEndpointRouteBuilder endpoints, string path = NuvToolsAccountDefaults.SettingsPath)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet(path, (HttpContext context) =>
        {
            var settings = context.RequestServices.GetRequiredService<IOptions<NuvToolsAccountClientSettings>>().Value;

            // A change of Account in the host's configuration must reach the next sign-in.
            context.Response.Headers.CacheControl = "no-store";

            return Results.Json(new { authority = settings.Authority, clientId = settings.ClientId });
        }).AllowAnonymous();
    }
}

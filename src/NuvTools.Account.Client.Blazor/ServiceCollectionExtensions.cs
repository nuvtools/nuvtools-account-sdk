using Microsoft.Extensions.DependencyInjection;
using NuvTools.AspNetCore.Blazor.Extensions;
using NuvTools.AspNetCore.Blazor.JSInterop;

namespace NuvTools.Account.Client.Blazor;

/// <summary>Registers <see cref="NuvToolsAccountSignIn"/>.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="NuvToolsAccountSignIn"/>, its named <see cref="HttpClient"/> — which
    /// carries no product token — <see cref="NuvToolsAccountBearerHandler"/> for the product API's
    /// client, and the session storage it keeps the sign-in state in, unless the app registered one
    /// already.
    /// </summary>
    public static IServiceCollection AddNuvToolsAccountSignIn(
        this IServiceCollection services, Action<NuvToolsAccountSignInOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new NuvToolsAccountSignInOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddScoped<NuvToolsAccountSignIn>();
        services.AddTransient<NuvToolsAccountBearerHandler>();
        services.AddHttpClient(NuvToolsAccountSignIn.HttpClientName);

        if (!services.Any(d => d.ServiceType == typeof(ISessionStorageService)))
            services.AddSessionStorageService();

        return services;
    }
}

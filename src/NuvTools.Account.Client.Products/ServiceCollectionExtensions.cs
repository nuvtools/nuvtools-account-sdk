using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NuvTools.Account.Client.Products;

/// <summary>Registers the Account's product API client and the gate.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="INuvToolsAccountProducts"/>, authenticated with <paramref name="credential"/>
    /// — the product's managed identity — and <see cref="NuvToolsAccountGate"/>.
    /// </summary>
    /// <remarks>
    /// Registered whether or not the options are filled in: unconfigured, every call fails without a
    /// request and the gate lets nobody in.
    /// </remarks>
    public static IServiceCollection AddNuvToolsAccountProducts(
        this IServiceCollection services, Action<NuvToolsAccountProductsOptions> configure, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(credential);

        var options = new NuvToolsAccountProductsOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient(_ => new NuvToolsAccountProductsTokenHandler(credential, options));

        services.AddHttpClient<INuvToolsAccountProducts, NuvToolsAccountProductsClient>(client =>
            {
                if (options.IsConfigured) client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

                client.Timeout = TimeSpan.FromSeconds(15);
            })
            .AddHttpMessageHandler<NuvToolsAccountProductsTokenHandler>();

        // Scoped, with the client it asks through; the answers live in the shared memory cache.
        services.AddScoped<NuvToolsAccountGate>();

        return services;
    }
}

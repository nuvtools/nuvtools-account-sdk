using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NuvTools.Account.Client.AspNetCore;

/// <summary>Registers the Account's API for products and the gate.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="INuvToolsAccountApi"/> and <see cref="NuvToolsAccountGate"/>.
    /// </summary>
    /// <param name="services">The product's services.</param>
    /// <param name="configure">The Account's address and the gate's cache.</param>
    /// <param name="token">
    /// How the product obtains its token for the Account's API — typically its managed identity asking
    /// Entra for <c>api://{tenant}/nuv-acc-products-{environment}/.default</c>.
    /// </param>
    /// <remarks>
    /// Registered whether or not the options are filled in: unconfigured, every call fails without a
    /// request and the gate lets nobody in.
    /// </remarks>
    public static IServiceCollection AddNuvToolsAccountApi(
        this IServiceCollection services,
        Action<NuvToolsAccountApiOptions> configure,
        Func<CancellationToken, ValueTask<string>> token)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(token);

        var options = new NuvToolsAccountApiOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient(_ => new NuvToolsAccountApiTokenHandler(token));

        services.AddHttpClient<INuvToolsAccountApi, NuvToolsAccountApiClient>(client =>
            {
                if (options.IsConfigured) client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

                client.Timeout = TimeSpan.FromSeconds(15);
            })
            .AddHttpMessageHandler<NuvToolsAccountApiTokenHandler>();

        // Scoped, with the client it asks through; the answers live in the shared memory cache.
        services.AddScoped<NuvToolsAccountGate>();

        return services;
    }
}

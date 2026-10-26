namespace NuvTools.Account.Client.Products;

/// <summary>Where the Account's product API is, how the product authenticates to it, and how long the gate's answers are kept.</summary>
public class NuvToolsAccountProductsOptions
{
    /// <summary>The Account's address — <c>https://account.nuvtools.com</c>. Empty leaves the client unconfigured: every call fails without a request.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The Entra scope the product's managed identity asks a token for — the Account's product API
    /// registration, <c>api://{tenant}/nuv-acc-products-{environment}/.default</c>.
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>How long an answer of the gate is used before the Account is asked again. A minute by default.</summary>
    public TimeSpan AccessCacheDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long the last answer of the gate is still used while the Account cannot be reached; after
    /// that the gate refuses. Five minutes by default; it never lets anybody in it has not seen.
    /// </summary>
    public TimeSpan StaleAccessLimit { get; set; } = TimeSpan.FromMinutes(5);

    public bool IsConfigured =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out _) && !string.IsNullOrWhiteSpace(Scope);
}

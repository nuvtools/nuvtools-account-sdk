namespace NuvTools.Account.Client.AspNetCore;

/// <summary>
/// What a product's web host tells its browser client about the Account: where it is and which
/// client registration to sign in as. Bind it from configuration, per environment.
/// </summary>
/// <remarks>
/// Served by the host rather than written into a WebAssembly app's own <c>appsettings.json</c>,
/// which is one static file inside the one image every environment runs.
/// </remarks>
public class NuvToolsAccountClientSettings
{
    /// <summary>The Account's issuer, for example <c>https://account.nuvtools.com/</c>.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>The browser client's registration at the Account, for example <c>dataprovider-portal</c>.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The Authority's origin, for the page's <c>connect-src</c> — the browser redeems the
    /// authorization code at the Account's token endpoint itself. <see langword="null"/> when no
    /// absolute Authority is configured.
    /// </summary>
    public string? AuthorityOrigin =>
        Uri.TryCreate(Authority, UriKind.Absolute, out var authority)
            ? authority.GetLeftPart(UriPartial.Authority)
            : null;
}

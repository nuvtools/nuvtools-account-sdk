namespace NuvTools.Account.Client.Blazor;

/// <summary>How a browser client signs in through Nuv Tools Account.</summary>
/// <remarks>
/// <para>
/// The Account's address and the client registration come from one of two places. Set
/// <see cref="Authority"/> and <see cref="ClientId"/> when they are the same in every environment;
/// leave them empty and the client reads them from <see cref="SettingsPath"/> on its own host,
/// which <c>NuvTools.Account.Client.AspNetCore</c>'s <c>MapNuvToolsAccountSettings</c> serves from
/// the host's configuration.
/// </para>
/// </remarks>
public class NuvToolsAccountSignInOptions
{
    /// <summary>The Account's issuer. Empty means read it from <see cref="SettingsPath"/>.</summary>
    public string? Authority { get; set; }

    /// <summary>The client registration at the Account. Empty means read it from <see cref="SettingsPath"/>.</summary>
    public string? ClientId { get; set; }

    /// <summary>Where the host serves the settings, relative to the app's base address.</summary>
    public string SettingsPath { get; set; } = "account-settings.json";

    /// <summary>
    /// The scopes asked for. The identity scopes by default; add the product API's scope — which is
    /// also the audience its API checks — for example <c>dataprovider-api</c>.
    /// </summary>
    public IList<string> Scopes { get; } = ["openid", "profile", "email"];

    /// <summary>
    /// Where the Account sends the browser back, relative to the app's base address. It must match
    /// a redirect address registered for the client at the Account, exactly.
    /// </summary>
    public string CallbackPath { get; set; } = "authentication/login-callback";

    /// <summary>
    /// Where the Account sends the browser after signing out, relative to the app's base address,
    /// and also registered for the client. The base address itself by default.
    /// </summary>
    public string PostSignOutPath { get; set; } = string.Empty;

    /// <summary>
    /// The local page to land on after signing in when none was asked for.
    /// </summary>
    public string DefaultReturnPath { get; set; } = "/";
}

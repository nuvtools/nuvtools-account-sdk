namespace NuvTools.Account.Client.AspNetCore;

/// <summary>Names a product uses when it signs people in through Nuv Tools Account.</summary>
public static class NuvToolsAccountDefaults
{
    /// <summary>
    /// The authentication scheme that accepts the Account's access tokens. A product accepts them on
    /// the one endpoint that opens its own session, and names the scheme there.
    /// </summary>
    public const string AuthenticationScheme = "NuvToolsAccount";

    /// <summary>
    /// The login provider a product records the Account's subject under, when it links its local
    /// users through ASP.NET Core Identity's external logins.
    /// </summary>
    public const string LoginProvider = "NuvToolsAccount";

    /// <summary>The display name for <see cref="LoginProvider"/>.</summary>
    public const string DisplayName = "Nuv Tools Account";

    /// <summary>Where the web host serves the Account's address to a browser client.</summary>
    public const string SettingsPath = "/account-settings.json";
}

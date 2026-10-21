namespace NuvTools.Account.Client.AspNetCore;

/// <summary>
/// Which Nuv Tools Account a product's API trusts, and for which audience.
/// </summary>
/// <remarks>
/// Left unconfigured, the scheme is still registered and refuses every token: a deployment that
/// forgot the setting cannot sign anybody in, rather than trusting whatever arrives.
/// </remarks>
public class NuvToolsAccountOptions
{
    /// <summary>
    /// The Account's issuer, for example <c>https://account.nuvtools.com/</c>. Its discovery
    /// document names the signing keys, and a token must carry exactly this as its <c>iss</c>.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The product API's audience: the scope its client is given at the Account, for example
    /// <c>dataprovider-api</c>. Checked, because every product's token comes from the same issuer
    /// and names the same person.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>How far the token's lifetime may be off the server's clock.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Whether enough is configured for the scheme to admit anybody at all.</summary>
    public bool IsConfigured =>
        Uri.TryCreate(Authority, UriKind.Absolute, out _) && !string.IsNullOrWhiteSpace(Audience);
}

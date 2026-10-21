using Microsoft.AspNetCore.Components;
using NuvTools.AspNetCore.Blazor.JSInterop;
using NuvTools.Common.ResultWrapper;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using IResult = NuvTools.Common.ResultWrapper.IResult;
using NuvMessages = NuvTools.Resources.Messages;

namespace NuvTools.Account.Client.Blazor;

/// <summary>
/// Signs a person in through Nuv Tools Account from a Blazor WebAssembly app: the authorization
/// code flow with PKCE, as a public client, and signing out through the Account's end-session page.
/// </summary>
/// <remarks>
/// <para>
/// The Account's access token is handed to the product once, on the callback, and not kept: the
/// product exchanges it for a session of its own, or uses it as it sees fit. No refresh token is
/// asked of the Account. The identity token is kept in session storage only to name the person
/// when signing out.
/// </para>
/// <para>
/// The <c>state</c> and the PKCE verifier stay in this tab's session storage between leaving and
/// coming back, and are spent on the way in: a response this tab did not ask for, or the same one
/// twice, is refused.
/// </para>
/// <para>Register it with <see cref="ServiceCollectionExtensions.AddNuvToolsAccountSignIn"/>.</para>
/// </remarks>
public class NuvToolsAccountSignIn(
    IHttpClientFactory httpClientFactory,
    ISessionStorageService sessionStorage,
    NavigationManager navigation,
    NuvToolsAccountSignInOptions options)
{
    /// <summary>The named client for the host and the Account: it carries no product token.</summary>
    public const string HttpClientName = "NuvToolsAccount";

    private const string PendingKey = "nuvtools.account.pendingSignIn";
    private const string IdentityTokenKey = "nuvtools.account.idToken";

    private NuvToolsAccountSettings? settings;
    private Task<IResult<NuvToolsAccountSignInResult>>? completion;

    /// <summary>
    /// Why the last sign-in in this tab did not complete, for the sign-in screen to show after the
    /// callback sends the browser back to it. Held here rather than put in an address, so nobody can
    /// send a link that makes the screen say anything.
    /// </summary>
    public string? LastFailure { get; set; }

    /// <summary>
    /// Set once signing out has begun. Ending the local session usually turns the page the person
    /// was on into the sign-in screen, which must not start a new sign-in and cancel the trip to the
    /// Account's end-session page: a sign-in screen checks this first.
    /// </summary>
    public bool SigningOut { get; private set; }

    /// <summary>
    /// Leaves for the Account's sign-in page, to come back to <paramref name="returnUrl"/> — a local
    /// path; anything else is replaced by <see cref="NuvToolsAccountSignInOptions.DefaultReturnPath"/>.
    /// </summary>
    public async Task<IResult> StartAsync(string? returnUrl)
    {
        var account = await GetSettingsAsync();

        if (account is null) return Result.Fail(NuvMessages.TheOperationCouldNotBePerformed);

        var pending = new PendingSignIn(Pkce.NewValue(), Pkce.NewValue(), LocalAddress.OrNull(returnUrl));

        await sessionStorage.SetItemAsync(PendingKey, pending);

        navigation.NavigateTo(AuthorizeAddress(account, pending), forceLoad: true);

        return Result.Success();
    }

    /// <summary>
    /// Finishes a sign-in on the callback page: checks the state, redeems the code at the Account,
    /// and passes the Account's access token to <paramref name="openSession"/> — the product's own
    /// step, typically exchanging it at its API. Answers with where to go next.
    /// </summary>
    /// <remarks>
    /// Once per page load, whoever asks. Opening a session usually switches the layout from an
    /// anonymous frame to a signed-in one, which builds the callback page again; the second instance
    /// receives the first one's answer instead of spending the already spent state a second time.
    /// </remarks>
    public Task<IResult<NuvToolsAccountSignInResult>> CompleteAsync(Uri callback, Func<string, Task<IResult>> openSession) =>
        completion ??= CompleteOnceAsync(callback, openSession);

    /// <summary>
    /// The Account's end-session address, which ends the Account's session too and sends the
    /// browser back to <see cref="NuvToolsAccountSignInOptions.PostSignOutPath"/>. Read it before
    /// clearing the local session: it names the person with the identity token kept from the
    /// sign-in. <see langword="null"/> when no Account is configured.
    /// </summary>
    public async Task<string?> SignOutAddressAsync()
    {
        var account = await GetSettingsAsync();

        if (account is null) return null;

        SigningOut = true;

        var identityToken = await sessionStorage.GetItemAsync<string>(IdentityTokenKey);
        await sessionStorage.RemoveItemAsync(IdentityTokenKey);

        return Address($"{account.Authority}connect/logout",
            ("client_id", account.ClientId),
            ("post_logout_redirect_uri", navigation.BaseUri + options.PostSignOutPath.TrimStart('/')),
            ("id_token_hint", identityToken));
    }

    /// <summary>
    /// The Account's page where a person manages their password, second factors, passkeys and
    /// linked sign-ins. <see langword="null"/> when no Account is configured.
    /// </summary>
    public async Task<string?> ManageAddressAsync() =>
        await GetSettingsAsync() is { } account ? $"{account.Authority}account/manage" : null;

    /// <summary>The Account and client registration in use, read once.</summary>
    public async Task<NuvToolsAccountSettings?> GetSettingsAsync()
    {
        if (settings is not null) return settings;

        var read = string.IsNullOrWhiteSpace(options.Authority) || string.IsNullOrWhiteSpace(options.ClientId)
            ? await ReadSettingsAsync()
            : new NuvToolsAccountSettings(options.Authority, options.ClientId);

        if (read is null
            || string.IsNullOrWhiteSpace(read.ClientId)
            || !Uri.TryCreate(read.Authority, UriKind.Absolute, out var authority)) return null;

        // The endpoints hang off the issuer, so it has to end in a slash.
        return settings = read with { Authority = authority.AbsoluteUri.TrimEnd('/') + "/" };
    }

    private async Task<IResult<NuvToolsAccountSignInResult>> CompleteOnceAsync(Uri callback, Func<string, Task<IResult>> openSession)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentNullException.ThrowIfNull(openSession);

        var query = HttpUtility.ParseQueryString(callback.Query);
        var pending = await sessionStorage.GetItemAsync<PendingSignIn>(PendingKey);

        // Spent whatever happens next: a second attempt with the same response must start over.
        await sessionStorage.RemoveItemAsync(PendingKey);

        if (pending is null
            || query["state"] is not { } state
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(pending.State)))
            return Fail(NuvMessages.AccessDenied);

        // The person cancelled, or the Account refused: there is no code to redeem.
        if (string.IsNullOrEmpty(query["code"])) return Fail(NuvMessages.AccessDenied);

        var account = await GetSettingsAsync();

        if (account is null) return Fail(NuvMessages.TheOperationCouldNotBePerformed);

        using var redeemed = await Client.PostAsync($"{account.Authority}connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = query["code"]!,
            ["redirect_uri"] = RedirectUri,
            ["client_id"] = account.ClientId!,
            ["code_verifier"] = pending.Verifier
        }));

        if (!redeemed.IsSuccessStatusCode) return Fail(NuvMessages.AccessDenied);

        var tokens = await redeemed.Content.ReadFromJsonAsync<AccountTokenResponse>();

        if (string.IsNullOrEmpty(tokens?.AccessToken)) return Fail(NuvMessages.AccessDenied);

        var session = await openSession(tokens.AccessToken);

        if (!session.Succeeded) return Result<NuvToolsAccountSignInResult>.Fail(session.Messages);

        if (!string.IsNullOrEmpty(tokens.IdentityToken))
            await sessionStorage.SetItemAsync(IdentityTokenKey, tokens.IdentityToken);

        return Result<NuvToolsAccountSignInResult>.Success(
            new NuvToolsAccountSignInResult(pending.ReturnUrl ?? options.DefaultReturnPath));
    }

    private string AuthorizeAddress(NuvToolsAccountSettings account, PendingSignIn pending) =>
        Address($"{account.Authority}connect/authorize",
            ("client_id", account.ClientId),
            ("response_type", "code"),
            ("scope", string.Join(' ', options.Scopes.Distinct(StringComparer.Ordinal))),
            ("redirect_uri", RedirectUri),
            ("state", pending.State),
            ("code_challenge", Pkce.Challenge(pending.Verifier)),
            ("code_challenge_method", "S256"));

    private async Task<NuvToolsAccountSettings?> ReadSettingsAsync()
    {
        try
        {
            return await Client.GetFromJsonAsync<NuvToolsAccountSettings>(
                new Uri(new Uri(navigation.BaseUri), options.SettingsPath));
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private HttpClient Client => httpClientFactory.CreateClient(HttpClientName);

    private string RedirectUri => navigation.BaseUri + options.CallbackPath.TrimStart('/');

    private static IResult<NuvToolsAccountSignInResult> Fail(string message) =>
        Result<NuvToolsAccountSignInResult>.Fail(message);

    /// <summary>An address with its query, leaving out the parameters that have no value.</summary>
    private static string Address(string path, params (string Name, string? Value)[] parameters) =>
        path + "?" + string.Join('&', parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value!)}"));
}

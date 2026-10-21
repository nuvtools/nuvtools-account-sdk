namespace NuvTools.Account.Client.Blazor;

/// <summary>
/// What a sign-in remembers across the trip to the Account: kept in the tab's session storage,
/// which survives the redirect and belongs to this tab alone.
/// </summary>
/// <param name="State">Sent with the request and expected back unchanged, so a response this tab did not ask for is refused.</param>
/// <param name="Verifier">The PKCE secret whose hash went with the request; only this tab can redeem the code.</param>
/// <param name="ReturnUrl">The local page to land on once signed in.</param>
internal sealed record PendingSignIn(string State, string Verifier, string? ReturnUrl);

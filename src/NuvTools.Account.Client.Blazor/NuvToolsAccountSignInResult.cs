namespace NuvTools.Account.Client.Blazor;

/// <summary>A completed sign-in: where to go next.</summary>
/// <param name="ReturnUrl">The local page that was asked for, or the default one.</param>
public record NuvToolsAccountSignInResult(string ReturnUrl);

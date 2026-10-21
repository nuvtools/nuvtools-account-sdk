namespace NuvTools.Account.Client.Blazor;

/// <summary>Which Account, and which client registration there, as resolved for this app.</summary>
/// <param name="Authority">The Account's issuer, ending in a slash.</param>
/// <param name="ClientId">The client registration.</param>
public record NuvToolsAccountSettings(string? Authority, string? ClientId);

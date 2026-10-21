namespace NuvTools.Account.Client.Blazor;

/// <summary>The Account API's answer to <c>GET me/contexts</c>: its result wrapper around the list.</summary>
internal sealed record AccountContextsResponse(bool Succeeded, IReadOnlyList<AccountContext>? Data);

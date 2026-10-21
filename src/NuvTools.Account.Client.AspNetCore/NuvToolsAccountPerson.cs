namespace NuvTools.Account.Client.AspNetCore;

/// <summary>Who a validated Nuv Tools Account token names.</summary>
/// <param name="Subject">The Account's identifier for the person; the key to link local records to.</param>
/// <param name="Email">The person's confirmed address.</param>
/// <param name="Name">The person's full name, when the token carries one.</param>
public record NuvToolsAccountPerson(string Subject, string Email, string? Name);

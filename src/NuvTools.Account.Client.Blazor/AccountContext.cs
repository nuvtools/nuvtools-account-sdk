namespace NuvTools.Account.Client.Blazor;

/// <summary>One context the Account offers the person; the role arrives as its number (1 Member, 2 Billing, 3 Owner).</summary>
internal sealed record AccountContext(string Kind, Guid? OrganizationId, string? OrganizationName, int? Role);

namespace NuvTools.Account.Client.Blazor;

/// <summary>One of the person's Account organizations, for an organization switcher.</summary>
/// <param name="OrganizationId">The Account organization; pass it to <see cref="NuvToolsAccountSignIn.SwitchAsync"/>.</param>
/// <param name="Name">The organization's name at the Account.</param>
/// <param name="Role">The person's role there: <c>Owner</c>, <c>Billing</c> or <c>Member</c>.</param>
public record NuvToolsAccountOrganization(Guid OrganizationId, string Name, string Role);

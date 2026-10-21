namespace NuvTools.Account.Client.AspNetCore;

/// <summary>
/// The values of <see cref="NuvToolsAccountClaims.OrganizationRole"/>. A person holds one per
/// organization, and each includes the ones before it: <see cref="Member"/>, <see cref="Billing"/>,
/// <see cref="Owner"/>.
/// </summary>
public static class NuvToolsAccountRoles
{
    public const string Member = "Member";

    public const string Billing = "Billing";

    public const string Owner = "Owner";
}

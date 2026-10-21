namespace NuvTools.Account.Client.AspNetCore;

/// <summary>
/// The context a Nuv Tools Account token was issued in: the person on their own, or acting inside
/// one of their organizations with the role they hold there.
/// </summary>
/// <param name="Kind"><see cref="NuvToolsAccountContextKinds.Personal"/> or <see cref="NuvToolsAccountContextKinds.Organization"/>.</param>
/// <param name="OrganizationId">The Account organization, in an organization context.</param>
/// <param name="Role">The person's role there (<see cref="NuvToolsAccountRoles"/>), in an organization context.</param>
/// <remarks>
/// The Account checks the membership when it issues the token and again on every refresh, so a
/// product can trust both values for the token's lifetime — and only for that.
/// </remarks>
public record NuvToolsAccountContext(string Kind, Guid? OrganizationId, string? Role)
{
    /// <summary>Whether the token acts inside an organization.</summary>
    public bool IsOrganization =>
        Kind == NuvToolsAccountContextKinds.Organization && OrganizationId is not null;
}

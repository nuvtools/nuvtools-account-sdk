using System.Security.Claims;

namespace NuvTools.Account.Client.AspNetCore;

/// <summary>Reads the person out of a principal the Nuv Tools Account scheme built.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The person the Account token names, or <see langword="null"/> when the principal lacks the
    /// subject or the address — which a validated Account token always carries.
    /// </summary>
    public static NuvToolsAccountPerson? GetNuvToolsAccountPerson(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.FindFirst(NuvToolsAccountClaims.Subject)?.Value;
        var email = principal.FindFirst(NuvToolsAccountClaims.Email)?.Value;

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email)) return null;

        var name = principal.FindFirst(NuvToolsAccountClaims.Name)?.Value;

        return new NuvToolsAccountPerson(subject, email, string.IsNullOrWhiteSpace(name) ? null : name);
    }

    /// <summary>
    /// The context the Account token was issued in. A token without the context claim, or with an
    /// organization context naming no valid organization, reads as personal.
    /// </summary>
    public static NuvToolsAccountContext GetNuvToolsAccountContext(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var kind = principal.FindFirst(NuvToolsAccountClaims.Context)?.Value;

        if (kind != NuvToolsAccountContextKinds.Organization
            || !Guid.TryParse(principal.FindFirst(NuvToolsAccountClaims.Organization)?.Value, out var organizationId))
            return new NuvToolsAccountContext(NuvToolsAccountContextKinds.Personal, null, null);

        var role = principal.FindFirst(NuvToolsAccountClaims.OrganizationRole)?.Value;

        return new NuvToolsAccountContext(NuvToolsAccountContextKinds.Organization, organizationId,
            string.IsNullOrWhiteSpace(role) ? null : role);
    }
}

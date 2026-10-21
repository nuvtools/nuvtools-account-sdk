namespace NuvTools.Account.Client.AspNetCore;

/// <summary>
/// The claims a Nuv Tools Account access token carries, by the names the Account gives them. The
/// scheme registered by <see cref="AuthenticationBuilderExtensions.AddNuvToolsAccount"/> reads them
/// unmapped, so these are the names to look up.
/// </summary>
public static class NuvToolsAccountClaims
{
    /// <summary>The Account's identifier for the person. Stable for life; link local records to it.</summary>
    public const string Subject = "sub";

    /// <summary>The person's address. Confirmed — the Account issues no token for an unconfirmed one — and unique there.</summary>
    public const string Email = "email";

    /// <summary>The person's full name.</summary>
    public const string Name = "name";

    public const string GivenName = "given_name";

    public const string FamilyName = "family_name";

    /// <summary>The context the session acts in: <c>personal</c> or <c>organization</c>.</summary>
    public const string Context = "ctx";

    /// <summary>The Account organization in context, when the context is an organization.</summary>
    public const string Organization = "org";

    /// <summary>The person's role in that organization: <c>Member</c>, <c>Billing</c> or <c>Owner</c>.</summary>
    public const string OrganizationRole = "org_role";

    /// <summary>How the person signed in (<c>pwd</c>, <c>otp</c>, <c>mfa</c>, <c>hwk</c>, <c>fed</c>); may repeat.</summary>
    public const string AuthenticationMethods = "amr";
}

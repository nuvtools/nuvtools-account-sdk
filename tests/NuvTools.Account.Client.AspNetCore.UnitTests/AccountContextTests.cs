using System.Security.Claims;

namespace NuvTools.Account.Client.AspNetCore.UnitTests;

/// <summary>The context a token was issued in, as a product reads it.</summary>
[TestFixture]
public class AccountContextTests
{
    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test"));

    [Test]
    public void AnOrganizationToken_NamesTheOrganizationAndTheRole()
    {
        var organizationId = Guid.NewGuid();

        var context = Principal(("ctx", "organization"), ("org", organizationId.ToString()), ("org_role", "Owner"))
            .GetNuvToolsAccountContext();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.IsOrganization, Is.True);
            Assert.That(context.OrganizationId, Is.EqualTo(organizationId));
            Assert.That(context.Role, Is.EqualTo(NuvToolsAccountRoles.Owner));
        }
    }

    [Test]
    public void APersonalToken_NamesNoOrganization()
    {
        var context = Principal(("ctx", "personal")).GetNuvToolsAccountContext();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.IsOrganization, Is.False);
            Assert.That(context.OrganizationId, Is.Null);
            Assert.That(context.Role, Is.Null);
        }
    }

    /// <summary>Anything short of a well-formed organization context is personal, never a guess.</summary>
    [TestCase(null, null)]
    [TestCase("organization", null)]
    [TestCase("organization", "not-a-guid")]
    [TestCase("something-else", "0199f5d2-0000-7000-8000-000000000001")]
    public void AnIncompleteContext_ReadsAsPersonal(string? kind, string? organization)
    {
        var claims = new List<(string, string)>();
        if (kind is not null) claims.Add(("ctx", kind));
        if (organization is not null) claims.Add(("org", organization));

        Assert.That(Principal([.. claims]).GetNuvToolsAccountContext().IsOrganization, Is.False);
    }
}

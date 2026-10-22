using System.Security.Claims;

namespace NuvTools.Account.Client.AspNetCore.UnitTests;

/// <summary>The plans a token carries, as a product reads them.</summary>
[TestFixture]
public class AccountPlanTests
{
    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test"));

    [Test]
    public void APlan_ComesWithWhatItGrants()
    {
        var plan = Principal(("plan", "clinuv:pro"), ("entitlements", """{"clinuv":{"seats":10}}"""))
            .GetNuvToolsAccountPlan("clinuv");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(plan, Is.Not.Null);
            Assert.That(plan!.PlanCode, Is.EqualTo("pro"));
            Assert.That(plan.Entitlements.GetProperty("seats").GetInt32(), Is.EqualTo(10));
        }
    }

    [Test]
    public void APlanWithoutEntitlements_GrantsAnEmptyObject()
    {
        var plan = Principal(("plan", "clinuv:basic")).GetNuvToolsAccountPlan("clinuv");

        Assert.That(plan!.Entitlements.EnumerateObject(), Is.Empty);
    }

    [Test]
    public void NoPlan_IsNotEntitled()
    {
        var principal = Principal(("sub", "person"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(principal.GetNuvToolsAccountPlans(), Is.Empty);
            Assert.That(principal.GetNuvToolsAccountPlan("clinuv"), Is.Null);
        }
    }

    [TestCase("clinuv")]
    [TestCase(":pro")]
    [TestCase("clinuv:")]
    public void AMalformedPlan_IsIgnored(string value)
    {
        Assert.That(Principal(("plan", value)).GetNuvToolsAccountPlans(), Is.Empty);
    }

    [Test]
    public void MalformedEntitlements_DoNotFailTheRead()
    {
        var plan = Principal(("plan", "clinuv:pro"), ("entitlements", "not json")).GetNuvToolsAccountPlan("clinuv");

        Assert.That(plan!.Entitlements.EnumerateObject(), Is.Empty);
    }
}

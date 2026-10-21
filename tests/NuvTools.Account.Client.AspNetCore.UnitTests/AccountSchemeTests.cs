using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace NuvTools.Account.Client.AspNetCore.UnitTests;

/// <summary>
/// The scheme admits a token the Account issued for this product and nothing else: another
/// audience, another issuer, another key, an expired token or no configuration at all are refused.
/// </summary>
[TestFixture]
public class AccountSchemeTests
{
    private static void Configured(NuvToolsAccountOptions options)
    {
        options.Authority = TestAccount.Authority;
        options.Audience = TestAccount.Audience;
    }

    [Test]
    public async Task ATokenForThisProduct_NamesThePerson()
    {
        await using var app = await TestHost.StartAsync(Configured);

        var response = await TestHost.SignInAsync(app, TestAccount.Token());
        var person = await response.Content.ReadFromJsonAsync<NuvToolsAccountPerson>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(person, Is.EqualTo(new NuvToolsAccountPerson(
                "4c1e0f2a-0000-4000-8000-000000000001", "person@nuvtools.test", "Ada Lovelace")));
        }
    }

    [Test]
    public async Task ATokenWithoutAName_StillNamesThePerson()
    {
        await using var app = await TestHost.StartAsync(Configured);

        var response = await TestHost.SignInAsync(app, TestAccount.Token(name: null));
        var person = await response.Content.ReadFromJsonAsync<NuvToolsAccountPerson>();

        Assert.That(person?.Name, Is.Null);
    }

    /// <summary>Every product's token comes from the same issuer: the audience is what tells them apart.</summary>
    [Test]
    public async Task ATokenForAnotherProduct_IsRefused()
    {
        await using var app = await TestHost.StartAsync(Configured);

        var response = await TestHost.SignInAsync(app, TestAccount.Token(audience: "account-api"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ATokenFromAnotherIssuer_IsRefused()
    {
        await using var app = await TestHost.StartAsync(Configured);

        var response = await TestHost.SignInAsync(app, TestAccount.Token(issuer: "https://elsewhere.nuvtools.test/"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ATokenSignedWithAnotherKey_IsRefused()
    {
        await using var app = await TestHost.StartAsync(Configured);

        var response = await TestHost.SignInAsync(app,
            TestAccount.Token(key: new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32))));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AnExpiredToken_IsRefused()
    {
        await using var app = await TestHost.StartAsync(Configured);

        var response = await TestHost.SignInAsync(app, TestAccount.Token(expires: DateTime.UtcNow.AddMinutes(-5)));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task NoToken_IsRefused()
    {
        await using var app = await TestHost.StartAsync(Configured);

        var response = await TestHost.SignInAsync(app, token: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>A deployment that forgot the setting signs nobody in, rather than trusting what arrives.</summary>
    [TestCase("", TestAccount.Audience)]
    [TestCase(TestAccount.Authority, "")]
    [TestCase("not-an-address", TestAccount.Audience)]
    public async Task Unconfigured_RefusesEveryToken(string authority, string audience)
    {
        await using var app = await TestHost.StartAsync(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
        }, trustTestKey: false);

        var response = await TestHost.SignInAsync(app, TestAccount.Token());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}

using Microsoft.AspNetCore.TestHost;
using System.Net;
using System.Text.Json;

namespace NuvTools.Account.Client.AspNetCore.UnitTests;

/// <summary>The host tells its browser client which Account to use, and never lets it be cached.</summary>
[TestFixture]
public class SettingsEndpointTests
{
    [Test]
    public async Task TheSettings_AreServedUncachedToAnybody()
    {
        await using var app = await TestHost.StartAsync(_ => { });

        var response = await app.GetTestClient().GetAsync(NuvToolsAccountDefaults.SettingsPath);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers.CacheControl?.NoStore, Is.True);
            Assert.That(body.RootElement.GetProperty("authority").GetString(), Is.EqualTo(TestAccount.Authority));
            Assert.That(body.RootElement.GetProperty("clientId").GetString(), Is.EqualTo("product-portal"));
        }
    }

    [TestCase("https://account.nuvtools.com/", "https://account.nuvtools.com")]
    [TestCase("https://localhost:7401/", "https://localhost:7401")]
    [TestCase("", null)]
    [TestCase("account.nuvtools.com", null)]
    public void TheAuthorityOrigin_IsWhatConnectSrcNames(string authority, string? origin) =>
        Assert.That(new NuvToolsAccountClientSettings { Authority = authority }.AuthorityOrigin, Is.EqualTo(origin));
}

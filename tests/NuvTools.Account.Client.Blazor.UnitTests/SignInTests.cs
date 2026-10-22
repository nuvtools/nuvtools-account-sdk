using NuvTools.Common.ResultWrapper;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace NuvTools.Account.Client.Blazor.UnitTests;

/// <summary>
/// The authorization code flow with PKCE as the browser runs it: the request that leaves for the
/// Account, the callback that comes back, and the way out.
/// </summary>
[TestFixture]
public class SignInTests
{
    private FakeNavigation navigation = null!;
    private FakeSessionStorage storage = null!;
    private FakeAccount account = null!;
    private NuvToolsAccountSignInOptions options = null!;

    [SetUp]
    public void CreateBrowser()
    {
        navigation = new FakeNavigation();
        storage = new FakeSessionStorage();
        account = new FakeAccount();
        options = new NuvToolsAccountSignInOptions { DefaultReturnPath = "/portal" };
        options.Scopes.Add("product-api");
    }

    [TearDown]
    public void Dispose() => account.Dispose();

    private NuvToolsAccountSignIn SignIn() => new(account, storage, navigation, options);

    private static Task<IResult> Opens(string _) => Task.FromResult(Result.Success());

    [Test]
    public async Task Starting_LeavesForTheAuthorizeEndpointWithAnS256Challenge()
    {
        var started = await SignIn().StartAsync("/portal/billing");

        var address = new Uri(navigation.LastAddress!);
        var query = HttpUtility.ParseQueryString(address.Query);
        var pending = storage.Items.Single().Value;
        var verifier = System.Text.Json.JsonDocument.Parse(pending).RootElement.GetProperty("Verifier").GetString()!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(started.Succeeded, Is.True);
            Assert.That(address.GetLeftPart(UriPartial.Path), Is.EqualTo($"{FakeAccount.Authority}connect/authorize"));
            Assert.That(query["client_id"], Is.EqualTo(FakeAccount.ClientId));
            Assert.That(query["response_type"], Is.EqualTo("code"));
            Assert.That(query["scope"], Is.EqualTo("openid profile email product-api"));
            Assert.That(query["redirect_uri"], Is.EqualTo($"{FakeNavigation.Base}authentication/login-callback"));
            Assert.That(query["code_challenge_method"], Is.EqualTo("S256"));
            Assert.That(query["code_challenge"], Is.EqualTo(S256(verifier)));
            Assert.That(query["state"], Is.Not.Empty);
            Assert.That(verifier, Has.Length.EqualTo(43));
        }
    }

    [Test]
    public async Task ConfiguredSettings_AreUsedWithoutAskingTheHost()
    {
        options.Authority = "https://configured.nuvtools.test";
        options.ClientId = "configured-client";
        account.SettingsJson = null;

        await SignIn().StartAsync(null);

        Assert.That(navigation.LastAddress, Does.StartWith("https://configured.nuvtools.test/connect/authorize?client_id=configured-client&"));
    }

    [Test]
    public async Task WithoutSettings_StartingFailsAndGoesNowhere()
    {
        account.SettingsJson = null;

        var started = await SignIn().StartAsync(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(started.Succeeded, Is.False);
            Assert.That(navigation.LastAddress, Is.Null);
        }
    }

    [Test]
    public async Task TheCallback_RedeemsTheCodeWithTheVerifier_AndHandsOverTheAccessToken()
    {
        var signIn = SignIn();
        await signIn.StartAsync("/portal/billing");
        var (state, verifier) = Pending();

        string? handedOver = null;
        var completed = await signIn.CompleteAsync(Callback(state), token =>
        {
            handedOver = token;
            return Task.FromResult(Result.Success());
        });

        var redemption = account.Redemptions.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(completed.Succeeded, Is.True);
            Assert.That(completed.Data!.ReturnUrl, Is.EqualTo("/portal/billing"));
            Assert.That(handedOver, Is.EqualTo(FakeAccount.AccessToken));
            Assert.That(redemption["grant_type"], Is.EqualTo("authorization_code"));
            Assert.That(redemption["code"], Is.EqualTo("the-code"));
            Assert.That(redemption["code_verifier"], Is.EqualTo(verifier));
            Assert.That(redemption["client_id"], Is.EqualTo(FakeAccount.ClientId));
            Assert.That(redemption["redirect_uri"], Is.EqualTo($"{FakeNavigation.Base}authentication/login-callback"));
        }
    }

    /// <summary>A response this tab did not ask for is refused before anything is redeemed.</summary>
    [Test]
    public async Task ACallbackWithAnotherState_IsRefused()
    {
        var signIn = SignIn();
        await signIn.StartAsync(null);

        var completed = await signIn.CompleteAsync(Callback("someone-elses-state"), Opens);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(completed.Succeeded, Is.False);
            Assert.That(account.Redemptions, Is.Empty);
        }
    }

    [Test]
    public async Task ACallbackNobodyStarted_IsRefused()
    {
        var completed = await SignIn().CompleteAsync(Callback("any"), Opens);

        Assert.That(completed.Succeeded, Is.False);
    }

    /// <summary>The state is spent on the way in: the same response cannot be replayed on a new page load.</summary>
    [Test]
    public async Task TheSameResponse_CannotBeUsedTwice()
    {
        await SignIn().StartAsync(null);
        var (state, _) = Pending();

        var first = await SignIn().CompleteAsync(Callback(state), Opens);
        var second = await SignIn().CompleteAsync(Callback(state), Opens);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.False);
            Assert.That(account.Redemptions, Has.Count.EqualTo(1));
        }
    }

    /// <summary>
    /// Opening the session rebuilds the callback page; the second instance gets the first one's
    /// answer from the same service instead of a refusal.
    /// </summary>
    [Test]
    public async Task OnOnePageLoad_TheCallbackCompletesOnce_WhoeverAsks()
    {
        var signIn = SignIn();
        await signIn.StartAsync(null);
        var (state, _) = Pending();

        var first = await signIn.CompleteAsync(Callback(state), Opens);
        var second = await signIn.CompleteAsync(Callback(state), Opens);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(account.Redemptions, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task ACancelledSignIn_RedeemsNothing()
    {
        var signIn = SignIn();
        await signIn.StartAsync(null);
        var (state, _) = Pending();

        var completed = await signIn.CompleteAsync(
            new Uri($"{FakeNavigation.Base}authentication/login-callback?error=access_denied&state={state}"), Opens);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(completed.Succeeded, Is.False);
            Assert.That(account.Redemptions, Is.Empty);
        }
    }

    [Test]
    public async Task ARefusedRedemption_OpensNoSession()
    {
        account.TokenStatus = System.Net.HttpStatusCode.BadRequest;
        var signIn = SignIn();
        await signIn.StartAsync(null);
        var (state, _) = Pending();
        var opened = false;

        var completed = await signIn.CompleteAsync(Callback(state), _ =>
        {
            opened = true;
            return Task.FromResult(Result.Success());
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(completed.Succeeded, Is.False);
            Assert.That(opened, Is.False);
        }
    }

    /// <summary>The product's own refusal is what the person is told.</summary>
    [Test]
    public async Task TheProductsRefusal_IsPassedOn()
    {
        var signIn = SignIn();
        await signIn.StartAsync(null);
        var (state, _) = Pending();

        var completed = await signIn.CompleteAsync(Callback(state), _ => Task.FromResult(Result.Fail("Inactive user.")));

        Assert.That(completed.Messages.Select(m => m.Title), Is.EqualTo(new[] { "Inactive user." }));
    }

    /// <summary>A crafted link cannot send a person who just signed in to another site.</summary>
    [TestCase("https://evil.test/")]
    [TestCase("//evil.test/")]
    [TestCase("/\\evil.test/")]
    [TestCase(null)]
    public async Task AReturnAddressOffThisApp_IsReplacedByTheDefault(string? returnUrl)
    {
        var signIn = SignIn();
        await signIn.StartAsync(returnUrl);
        var (state, _) = Pending();

        var completed = await signIn.CompleteAsync(Callback(state), Opens);

        Assert.That(completed.Data!.ReturnUrl, Is.EqualTo("/portal"));
    }

    [Test]
    public async Task SigningOut_NamesThePersonAndComesBackHere()
    {
        var signIn = SignIn();
        await signIn.StartAsync(null);
        var (state, _) = Pending();
        await signIn.CompleteAsync(Callback(state), Opens);

        var address = new Uri((await signIn.SignOutAddressAsync())!);
        var query = HttpUtility.ParseQueryString(address.Query);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signIn.SigningOut, Is.True);
            Assert.That(address.GetLeftPart(UriPartial.Path), Is.EqualTo($"{FakeAccount.Authority}connect/logout"));
            Assert.That(query["client_id"], Is.EqualTo(FakeAccount.ClientId));
            Assert.That(query["post_logout_redirect_uri"], Is.EqualTo(FakeNavigation.Base));
            Assert.That(query["id_token_hint"], Is.EqualTo("account-id-token"));
        }
    }

    [Test]
    public async Task Starting_AsksForNoContextAndShowsTheSignInPage()
    {
        await SignIn().StartAsync(null);

        var query = HttpUtility.ParseQueryString(new Uri(navigation.LastAddress!).Query);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(query["context"], Is.Null);
            Assert.That(query["prompt"], Is.Null);
        }
    }

    /// <summary>Switching asks the Account for that organization, without showing a sign-in page.</summary>
    [Test]
    public async Task Switching_AsksForTheOrganizationSilently()
    {
        var organizationId = Guid.NewGuid();

        await SignIn().SwitchAsync(organizationId, "/portal/members");

        var query = HttpUtility.ParseQueryString(new Uri(navigation.LastAddress!).Query);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(query["context"], Is.EqualTo(organizationId.ToString()));
            Assert.That(query["prompt"], Is.EqualTo("none"));
        }
    }

    [Test]
    public async Task SwitchingToNoOrganization_AsksForThePersonalContext()
    {
        await SignIn().SwitchAsync(null, null);

        Assert.That(HttpUtility.ParseQueryString(new Uri(navigation.LastAddress!).Query)["context"], Is.EqualTo("personal"));
    }

    [Test]
    public async Task TheOrganizations_AreReadFromTheAccountWithTheTokenOfTheSignIn()
    {
        var signIn = SignIn();
        await signIn.StartAsync(null);
        var (state, _) = Pending();
        await signIn.CompleteAsync(Callback(state), Opens);

        var organizations = await signIn.GetOrganizationsAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(organizations.Succeeded, Is.True);
            Assert.That(account.LastApiBearer, Is.EqualTo(FakeAccount.AccessToken));
            Assert.That(organizations.Data!.Select(o => (o.Name, o.Role)),
                Is.EqualTo(new[] { ("Acme", "Owner"), ("Globex", "Member") }));
        }
    }

    /// <summary>
    /// Opening the session raises the authentication change an organization switcher reacts to; the
    /// list it reads right then must already find the token.
    /// </summary>
    [Test]
    public async Task TheOrganizations_CanBeRead_WhileTheSessionOpens()
    {
        var signIn = SignIn();
        await signIn.StartAsync(null);
        var (state, _) = Pending();
        NuvTools.Common.ResultWrapper.IResult<IReadOnlyList<NuvToolsAccountOrganization>>? duringOpen = null;

        await signIn.CompleteAsync(Callback(state), async _ =>
        {
            duringOpen = await signIn.GetOrganizationsAsync();
            return Result.Success();
        });

        Assert.That(duringOpen?.Data?.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ARefusedSession_KeepsNoToken()
    {
        var signIn = SignIn();
        await signIn.StartAsync(null);
        var (state, _) = Pending();

        await signIn.CompleteAsync(Callback(state), _ => Task.FromResult(Result.Fail("Inactive user.")));

        Assert.That((await signIn.GetOrganizationsAsync()).Succeeded, Is.False);
    }

    [Test]
    public async Task TheOrganizations_AreNotAskedFor_WithAnExpiredToken()
    {
        account.IssuedAccessToken = FakeAccount.Jwt(DateTimeOffset.UtcNow.AddMinutes(-1));
        var signIn = SignIn();
        await signIn.StartAsync(null);
        var (state, _) = Pending();
        await signIn.CompleteAsync(Callback(state), Opens);

        var organizations = await signIn.GetOrganizationsAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(organizations.Succeeded, Is.False);
            Assert.That(account.LastApiBearer, Is.Null);
        }
    }

    [Test]
    public async Task TheOrganizations_AreNotAskedFor_BeforeAnySignIn()
    {
        var organizations = await SignIn().GetOrganizationsAsync();

        Assert.That(organizations.Succeeded, Is.False);
    }

    [Test]
    public async Task TheManagePage_IsTheAccounts()
    {
        Assert.That(await SignIn().ManageAddressAsync(), Is.EqualTo($"{FakeAccount.Authority}account/manage"));
    }

    private (string State, string Verifier) Pending()
    {
        var pending = System.Text.Json.JsonDocument.Parse(storage.Items["nuvtools.account.pendingSignIn"]).RootElement;

        return (pending.GetProperty("State").GetString()!, pending.GetProperty("Verifier").GetString()!);
    }

    private static Uri Callback(string state) =>
        new($"{FakeNavigation.Base}authentication/login-callback?code=the-code&state={Uri.EscapeDataString(state)}&iss=https%3A%2F%2Faccount.nuvtools.test%2F");

    private static string S256(string verifier) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

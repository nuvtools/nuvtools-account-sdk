using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace NuvTools.Account.Client.Products.UnitTests;

/// <summary>
/// The product API client as a product uses it — the requests it sends, and how it tells "no" from
/// "could not ask" — and the gate on top of it, with its cache.
/// </summary>
[TestFixture]
public class ProductsClientTests
{
    private static readonly Guid Person = Guid.Parse("0199f5d2-0000-7000-8000-0000000000a1");
    private static readonly Guid Organization = Guid.Parse("0199f5d2-0000-7000-8000-0000000000b1");
    private static readonly Guid Enrollment = Guid.Parse("0199f5d2-0000-7000-8000-0000000000c1");

    private const string AccessJson = """
        {"succeeded":true,"data":{"enrollmentId":"0199f5d2-0000-7000-8000-0000000000c1","status":1,"license":{"code":"professional","billed":true},"version":3}}
        """;

    private const string NotFoundJson = """{"succeeded":false,"containsNotFound":true,"messages":[{"title":"No access."}]}""";

    private StubAccount account = null!;
    private ManualClock clock = null!;
    private NuvToolsAccountProductsOptions options = null!;

    [SetUp]
    public void CreateAccount()
    {
        account = new StubAccount();
        clock = new ManualClock();
        options = new NuvToolsAccountProductsOptions { BaseUrl = "https://account.nuvtools.test", Scope = "api://tenant/nuv-acc-products-test/.default" };
    }

    [TearDown]
    public void Dispose() => account.Dispose();

    private NuvToolsAccountProductsClient Client() =>
        new(new HttpClient(account, disposeHandler: false) { BaseAddress = new Uri("https://account.nuvtools.test/") }, options,
            NullLogger<NuvToolsAccountProductsClient>.Instance);

    private NuvToolsAccountGate Gate(MemoryCache cache) => new(Client(), cache, options, clock);

    [Test]
    public async Task TheGateAnswer_IsReadFromTheAccessRoute()
    {
        account.Body = AccessJson;

        var access = await Client().GetAccessAsync(Person, Organization);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(access.Succeeded, Is.True);
            Assert.That(access.Data!.EnrollmentId, Is.EqualTo(Enrollment));
            Assert.That(access.Data.License, Is.EqualTo(new NuvToolsAccountLicense("professional", true)));
            Assert.That(account.Requests.Single().PathAndQuery, Is.EqualTo($"/api/v1/products/me/access?personId={Person}&organizationId={Organization}"));
        }
    }

    [Test]
    public async Task ARefusal_IsNotFound_AndAnUnreachableAccount_IsNot()
    {
        account.Status = HttpStatusCode.NotFound;
        account.Body = NotFoundJson;
        var refused = await Client().GetAccessAsync(Person, null);

        account.Unreachable = true;
        var unreachable = await Client().GetAccessAsync(Person, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refused.ContainsNotFound, Is.True);
            Assert.That(unreachable.Succeeded, Is.False);
            Assert.That(unreachable.ContainsNotFound, Is.False);
        }
    }

    [Test]
    public async Task Writes_NameTheActingPerson()
    {
        account.Body = """{"succeeded":true,"data":{"enrollmentId":"0199f5d2-0000-7000-8000-0000000000c1","organizationId":"0199f5d2-0000-7000-8000-0000000000b1","personId":null,"status":1,"enabledByPersonId":"0199f5d2-0000-7000-8000-0000000000a1","enabledOnUtc":"2026-10-25T12:00:00+00:00"}}""";

        var enrolled = await Client().EnrollAsync(Person, Organization, null);

        var sent = account.Requests.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(enrolled.Data!.OrganizationId, Is.EqualTo(Organization));
            Assert.That(sent.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(sent.PathAndQuery, Is.EqualTo("/api/v1/products/me/enrollments"));
            Assert.That(sent.ActingPerson, Is.EqualTo(Person.ToString()));
        }
    }

    [Test]
    public async Task Unconfigured_SendsNothing()
    {
        options.BaseUrl = string.Empty;

        var access = await Client().GetAccessAsync(Person, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(access.Succeeded, Is.False);
            Assert.That(account.Requests, Is.Empty);
        }
    }

    [Test]
    public async Task TheGate_KeepsItsAnswerForAMinute_ThenAsksAgain()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        account.Body = AccessJson;

        var first = await Gate(cache).CheckAsync(Person, Organization);
        clock.Now = clock.Now.AddSeconds(30);
        var cached = await Gate(cache).CheckAsync(Person, Organization);
        clock.Now = clock.Now.AddSeconds(40);
        var asked = await Gate(cache).CheckAsync(Person, Organization);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(cached, Is.EqualTo(first));
            Assert.That(asked, Is.EqualTo(first));
            Assert.That(account.Requests, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task TheGate_KeepsAno_Too()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        account.Status = HttpStatusCode.NotFound;
        account.Body = NotFoundJson;

        var refused = await Gate(cache).CheckAsync(Person, Organization);
        var again = await Gate(cache).CheckAsync(Person, Organization);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refused, Is.Null);
            Assert.That(again, Is.Null);
            Assert.That(account.Requests, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task WhenTheAccountIsUnreachable_TheLastAnswerStandsForFiveMinutes_ThenTheGateRefuses()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        account.Body = AccessJson;
        await Gate(cache).CheckAsync(Person, Organization);

        account.Unreachable = true;
        clock.Now = clock.Now.AddMinutes(2);
        var stale = await Gate(cache).CheckAsync(Person, Organization);
        clock.Now = clock.Now.AddMinutes(4);
        var refused = await Gate(cache).CheckAsync(Person, Organization);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stale, Is.Not.Null);
            Assert.That(refused, Is.Null);
        }
    }

    [Test]
    public async Task TheGate_NeverLetsInSomebodyItHasNotSeen_WhileTheAccountIsUnreachable()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        account.Unreachable = true;

        Assert.That(await Gate(cache).CheckAsync(Person, Organization), Is.Null);
    }

    [Test]
    public async Task Evicting_MakesTheNextCheckAsk()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        account.Body = AccessJson;
        var gate = Gate(cache);

        await gate.CheckAsync(Person, null);
        gate.Evict(Person, null);
        await gate.CheckAsync(Person, null);

        Assert.That(account.Requests, Has.Count.EqualTo(2));
    }
}

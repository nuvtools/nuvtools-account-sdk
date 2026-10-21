using System.Net;
using System.Text;
using System.Web;

namespace NuvTools.Account.Client.Blazor.UnitTests;

/// <summary>
/// Answers the HTTP calls the sign-in makes — the host's settings and the Account's token endpoint —
/// and records the redemptions it received.
/// </summary>
internal sealed class FakeAccount : HttpMessageHandler, IHttpClientFactory
{
    public const string Authority = "https://account.nuvtools.test/";

    public const string ClientId = "product-portal";

    public List<Dictionary<string, string?>> Redemptions { get; } = [];

    public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;

    public string? SettingsJson { get; set; } = $$"""{"authority":"{{Authority}}","clientId":"{{ClientId}}"}""";

    /// <summary>A JWT-shaped access token, valid for an hour; nothing here checks its signature.</summary>
    public static readonly string AccessToken = Jwt(DateTimeOffset.UtcNow.AddHours(1));

    /// <summary>What the token endpoint hands out; replace it with an expired one to test expiry.</summary>
    public string IssuedAccessToken { get; set; } = AccessToken;

    /// <summary>The bearer token the last call to the Account's API carried.</summary>
    public string? LastApiBearer { get; private set; }

    public static string Jwt(DateTimeOffset expires)
    {
        static string Encode(string json) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var header = Encode("""{"alg":"none","typ":"at+jwt"}""");
        var payload = Encode("{\"sub\":\"person\",\"exp\":" + expires.ToUnixTimeSeconds() + "}");

        return $"{header}.{payload}.signature";
    }

    public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var address = request.RequestUri!.ToString();

        if (address == $"{FakeNavigation.Base}account-settings.json")
            return SettingsJson is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : Json(HttpStatusCode.OK, SettingsJson);

        if (address == $"{Authority}connect/token")
        {
            var form = HttpUtility.ParseQueryString(await request.Content!.ReadAsStringAsync(cancellationToken));
            Redemptions.Add(form.AllKeys.ToDictionary(k => k!, k => form[k]));

            return Json(TokenStatus, $$"""{"access_token":"{{IssuedAccessToken}}","id_token":"account-id-token","token_type":"Bearer"}""");
        }

        if (address == $"{Authority}api/v1/me/contexts")
        {
            LastApiBearer = request.Headers.Authorization?.Parameter;

            return Json(HttpStatusCode.OK, """
                {"succeeded":true,"data":[
                  {"kind":"personal","organizationId":null,"organizationName":null,"role":null},
                  {"kind":"organization","organizationId":"0199f5d2-0000-7000-8000-000000000001","organizationName":"Acme","role":3},
                  {"kind":"organization","organizationId":"0199f5d2-0000-7000-8000-000000000002","organizationName":"Globex","role":1}
                ]}
                """);
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}

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

            return Json(TokenStatus, """{"access_token":"account-access-token","id_token":"account-id-token","token_type":"Bearer"}""");
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}

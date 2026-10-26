using System.Net;
using System.Text;

namespace NuvTools.Account.Client.AspNetCore.UnitTests;

/// <summary>Stands in for the Account's product API: answers every request with what a test set, and records what was sent.</summary>
internal sealed class StubAccount : HttpMessageHandler
{
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    public string Body { get; set; } = """{"succeeded":true,"data":null}""";

    /// <summary>Thrown instead of answering, as when the Account cannot be reached.</summary>
    public bool Unreachable { get; set; }

    public List<(HttpMethod Method, string PathAndQuery, string? ActingPerson, string? Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((request.Method, request.RequestUri!.PathAndQuery,
            request.Headers.TryGetValues(NuvToolsAccountApiClient.ActingPersonHeader, out var acting) ? acting.Single() : null,
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));

        if (Unreachable) throw new HttpRequestException("No route to the Account.");

        return new HttpResponseMessage(Status) { Content = new StringContent(Body, Encoding.UTF8, "application/json") };
    }
}

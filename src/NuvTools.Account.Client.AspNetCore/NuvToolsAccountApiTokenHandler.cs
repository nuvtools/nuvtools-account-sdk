using System.Net.Http.Headers;

namespace NuvTools.Account.Client.AspNetCore;

/// <summary>
/// Adds the product's own token to every request to the Account's API — whatever the product's
/// token function returns, typically its managed identity's Entra token for the Account's product API.
/// </summary>
/// <remarks>
/// The package knows no identity provider: the product says how the token is obtained, and caches it
/// if its credential does not.
/// </remarks>
public class NuvToolsAccountApiTokenHandler(Func<CancellationToken, ValueTask<string>> token) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await token(cancellationToken));

        return await base.SendAsync(request, cancellationToken);
    }
}

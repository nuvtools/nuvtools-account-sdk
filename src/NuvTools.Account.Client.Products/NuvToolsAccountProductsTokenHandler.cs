using Azure.Core;
using System.Net.Http.Headers;

namespace NuvTools.Account.Client.Products;

/// <summary>
/// Adds the product's Entra token for the Account's product API to every request: its managed
/// identity asking for <see cref="NuvToolsAccountProductsOptions.Scope"/>.
/// </summary>
/// <remarks>The credential caches the token and renews it before it expires, so asking on every request reaches Entra about once an hour.</remarks>
public class NuvToolsAccountProductsTokenHandler(TokenCredential credential, NuvToolsAccountProductsOptions options) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await credential.GetTokenAsync(new TokenRequestContext([options.Scope]), cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        return await base.SendAsync(request, cancellationToken);
    }
}

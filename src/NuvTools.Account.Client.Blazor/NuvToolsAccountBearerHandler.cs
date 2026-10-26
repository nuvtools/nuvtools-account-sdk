using System.Net.Http.Headers;

namespace NuvTools.Account.Client.Blazor;

/// <summary>
/// Puts this tab's Account access token on every request to the product's API, renewed first when it
/// is about to expire. A request goes without one when there is no session; the API then answers 401.
/// </summary>
/// <remarks>
/// Add it to the product API's client with <c>AddHttpMessageHandler&lt;NuvToolsAccountBearerHandler&gt;()</c>;
/// <see cref="ServiceCollectionExtensions.AddNuvToolsAccountSignIn"/> registers it.
/// </remarks>
public class NuvToolsAccountBearerHandler(NuvToolsAccountSignIn signIn) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.Authorization is null && await signIn.GetAccessTokenAsync(cancellationToken) is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}

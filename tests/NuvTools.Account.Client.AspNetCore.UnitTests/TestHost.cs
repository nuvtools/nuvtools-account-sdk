using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace NuvTools.Account.Client.AspNetCore.UnitTests;

/// <summary>
/// A host with the scheme registered as a product registers it, one endpoint behind it that echoes
/// the person, and the settings endpoint.
/// </summary>
internal static class TestHost
{
    public const string SignInPath = "/sign-in";

    public static async Task<WebApplication> StartAsync(Action<NuvToolsAccountOptions> configure, bool trustTestKey = true)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddAuthentication().AddNuvToolsAccount(configure);
        builder.Services.AddAuthorization();

        builder.Services.Configure<NuvToolsAccountClientSettings>(settings =>
        {
            settings.Authority = TestAccount.Authority;
            settings.ClientId = "product-portal";
        });

        // The keys the Account would publish, replaced by the test's own. A fixed configuration
        // makes the handler build a static configuration manager, so nothing reaches the network,
        // and the issuer, audience and lifetime checks run unchanged.
        if (trustTestKey)
        {
            builder.Services.Configure<JwtBearerOptions>(NuvToolsAccountDefaults.AuthenticationScheme, bearer =>
            {
                if (bearer.Authority is not null)
                    bearer.Configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = TestAccount.Authority,
                        SigningKeys = { TestAccount.SigningKey }
                    };
            });
        }

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapPost(SignInPath, [Authorize(AuthenticationSchemes = NuvToolsAccountDefaults.AuthenticationScheme)]
            (HttpContext context) => Results.Json(context.User.GetNuvToolsAccountPerson()));

        app.MapNuvToolsAccountSettings();

        await app.StartAsync();

        return app;
    }

    public static Task<HttpResponseMessage> SignInAsync(WebApplication app, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SignInPath);

        if (token is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return app.GetTestClient().SendAsync(request);
    }
}

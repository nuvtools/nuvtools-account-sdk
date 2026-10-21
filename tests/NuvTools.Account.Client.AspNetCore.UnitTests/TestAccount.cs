using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace NuvTools.Account.Client.AspNetCore.UnitTests;

/// <summary>
/// Stands in for Nuv Tools Account: mints access tokens shaped like its own, signed with a key the
/// test host trusts in place of the Account's published ones.
/// </summary>
internal static class TestAccount
{
    public const string Authority = "https://account.nuvtools.test/";

    public const string Audience = "product-api";

    public static readonly SecurityKey SigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));

    public static string Token(
        string subject = "4c1e0f2a-0000-4000-8000-000000000001",
        string email = "person@nuvtools.test",
        string? name = "Ada Lovelace",
        string audience = Audience,
        string issuer = Authority,
        SecurityKey? key = null,
        DateTime? expires = null)
    {
        var claims = new Dictionary<string, object> { ["sub"] = subject, ["email"] = email };

        if (name is not null) claims["name"] = name;

        var expiry = expires ?? DateTime.UtcNow.AddMinutes(5);

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            NotBefore = expiry.AddMinutes(-10),
            Expires = expiry,
            SigningCredentials = new SigningCredentials(key ?? SigningKey, SecurityAlgorithms.HmacSha256)
        });
    }
}

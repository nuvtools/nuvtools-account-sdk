using System.Security.Cryptography;
using System.Text;

namespace NuvTools.Account.Client.Blazor;

/// <summary>The random values and the S256 challenge of RFC 7636.</summary>
internal static class Pkce
{
    /// <summary>32 random bytes, base64url: the length RFC 7636 recommends for a verifier.</summary>
    public static string NewValue() => Base64Url(RandomNumberGenerator.GetBytes(32));

    /// <summary>BASE64URL(SHA256(ASCII(verifier))).</summary>
    public static string Challenge(string verifier) =>
        Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    /// <summary>Written out rather than taken from System.Buffers.Text, which net8.0 lacks.</summary>
    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

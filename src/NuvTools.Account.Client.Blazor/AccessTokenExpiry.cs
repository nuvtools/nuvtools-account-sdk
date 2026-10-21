using System.Text;
using System.Text.Json;

namespace NuvTools.Account.Client.Blazor;

/// <summary>
/// Reads a JWT's <c>exp</c> without validating it: the browser holds no key to validate with, and
/// only needs to know whether the token is worth sending.
/// </summary>
internal static class AccessTokenExpiry
{
    /// <summary>Whether the token has expired, or is not a JWT whose expiry can be read.</summary>
    public static bool HasExpired(string token, DateTimeOffset now)
    {
        var parts = token.Split('.');

        if (parts.Length != 3) return true;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var document = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));

            return !document.RootElement.TryGetProperty("exp", out var exp)
                || !exp.TryGetInt64(out var seconds)
                || DateTimeOffset.FromUnixTimeSeconds(seconds) <= now.AddSeconds(30);
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return true;
        }
    }
}

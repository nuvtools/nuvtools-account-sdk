using System.Text.Json.Serialization;

namespace NuvTools.Account.Client.Blazor;

/// <summary>The part of the Account's token response the client uses.</summary>
internal sealed record AccountTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("id_token")] string? IdentityToken,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null);

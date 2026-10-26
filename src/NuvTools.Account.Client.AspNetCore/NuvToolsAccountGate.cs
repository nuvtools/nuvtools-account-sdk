using Microsoft.Extensions.Caching.Memory;

namespace NuvTools.Account.Client.AspNetCore;

/// <summary>
/// Whether a person may use the product now, asked of the Account on a request and kept for a short
/// while — the gate a licence is in Microsoft 365. The product then decides, from its own roles, what
/// they may do.
/// </summary>
/// <remarks>
/// <para>
/// An answer — yes or no — is used for <see cref="NuvToolsAccountApiOptions.AccessCacheDuration"/>.
/// When the Account cannot be reached, the last answer is still used up to
/// <see cref="NuvToolsAccountApiOptions.StaleAccessLimit"/>, and after that the gate refuses: it
/// never fails open, and never lets in anybody it has not seen.
/// </para>
/// <para>
/// A change the product makes itself — a licence given, an organization enrolled — is seen at once
/// after <see cref="Evict"/>; one made elsewhere, within the cache's duration.
/// </para>
/// </remarks>
public class NuvToolsAccountGate(
    INuvToolsAccountApi products,
    IMemoryCache cache,
    NuvToolsAccountApiOptions options,
    TimeProvider time)
{
    private sealed record Entry(NuvToolsAccountAccess? Access, DateTimeOffset AskedOnUtc);

    /// <summary>The answer, or <see langword="null"/> when the person may not use the product — or the Account could not say for too long.</summary>
    public async Task<NuvToolsAccountAccess?> CheckAsync(Guid personId, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        var key = Key(personId, organizationId);
        var now = time.GetUtcNow();

        if (cache.TryGetValue<Entry>(key, out var held) && held is not null && now - held.AskedOnUtc < options.AccessCacheDuration)
            return held.Access;

        var answer = await products.GetAccessAsync(personId, organizationId, cancellationToken);

        if (answer.Succeeded || answer.ContainsNotFound)
        {
            var access = answer.Succeeded ? answer.Data : null;

            cache.Set(key, new Entry(access, now), new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = options.StaleAccessLimit, Size = 1 });

            return access;
        }

        // The Account could not be asked: the last answer stands for a while, never a new one.
        return held is not null && now - held.AskedOnUtc < options.StaleAccessLimit ? held.Access : null;
    }

    /// <summary>Forgets the answer for a person, so the next request asks again — after the product changed something itself.</summary>
    public void Evict(Guid personId, Guid? organizationId) => cache.Remove(Key(personId, organizationId));

    private static string Key(Guid personId, Guid? organizationId) => $"nuvtools.account.access:{personId:N}:{organizationId?.ToString("N") ?? "personal"}";
}

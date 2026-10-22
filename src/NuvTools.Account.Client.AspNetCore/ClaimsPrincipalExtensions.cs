using System.Security.Claims;
using System.Text.Json;

namespace NuvTools.Account.Client.AspNetCore;

/// <summary>Reads the person out of a principal the Nuv Tools Account scheme built.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The person the Account token names, or <see langword="null"/> when the principal lacks the
    /// subject or the address — which a validated Account token always carries.
    /// </summary>
    public static NuvToolsAccountPerson? GetNuvToolsAccountPerson(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.FindFirst(NuvToolsAccountClaims.Subject)?.Value;
        var email = principal.FindFirst(NuvToolsAccountClaims.Email)?.Value;

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email)) return null;

        var name = principal.FindFirst(NuvToolsAccountClaims.Name)?.Value;

        return new NuvToolsAccountPerson(subject, email, string.IsNullOrWhiteSpace(name) ? null : name);
    }

    /// <summary>
    /// The context the Account token was issued in. A token without the context claim, or with an
    /// organization context naming no valid organization, reads as personal.
    /// </summary>
    public static NuvToolsAccountContext GetNuvToolsAccountContext(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var kind = principal.FindFirst(NuvToolsAccountClaims.Context)?.Value;

        if (kind != NuvToolsAccountContextKinds.Organization
            || !Guid.TryParse(principal.FindFirst(NuvToolsAccountClaims.Organization)?.Value, out var organizationId))
            return new NuvToolsAccountContext(NuvToolsAccountContextKinds.Personal, null, null);

        var role = principal.FindFirst(NuvToolsAccountClaims.OrganizationRole)?.Value;

        return new NuvToolsAccountContext(NuvToolsAccountContextKinds.Organization, organizationId,
            string.IsNullOrWhiteSpace(role) ? null : role);
    }

    /// <summary>
    /// The plans the token's context is subscribed to, among the products whose APIs the token is for.
    /// Empty when it subscribes to none — which a product reads as "not entitled".
    /// </summary>
    public static IReadOnlyList<NuvToolsAccountPlan> GetNuvToolsAccountPlans(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var granted = ReadEntitlements(principal.FindFirst(NuvToolsAccountClaims.Entitlements)?.Value);

        return
        [
            .. principal.FindAll(NuvToolsAccountClaims.Plan)
                .Select(claim => claim.Value.Split(':', 2))
                .Where(parts => parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0)
                .Select(parts => new NuvToolsAccountPlan(
                    parts[0],
                    parts[1],
                    granted.TryGetValue(parts[0], out var entitlements) ? entitlements : EmptyObject))
        ];
    }

    /// <summary>The plan of one product, or <see langword="null"/> when the context does not subscribe to it.</summary>
    public static NuvToolsAccountPlan? GetNuvToolsAccountPlan(this ClaimsPrincipal principal, string productCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);

        return principal.GetNuvToolsAccountPlans()
            .FirstOrDefault(plan => string.Equals(plan.ProductCode, productCode, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>The <c>entitlements</c> claim, by product; nothing when it is absent or not a JSON object.</summary>
    private static Dictionary<string, JsonElement> ReadEntitlements(string? json)
    {
        var byProduct = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(json)) return byProduct;

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object) return byProduct;

            foreach (var product in document.RootElement.EnumerateObject())
                byProduct[product.Name] = product.Value.Clone();
        }
        catch (JsonException)
        {
        }

        return byProduct;
    }
}

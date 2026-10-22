using System.Text.Json;

namespace NuvTools.Account.Client.AspNetCore;

/// <summary>
/// A plan the token's context is subscribed to, for a product whose API the token was issued for.
/// </summary>
/// <param name="ProductCode">The product — <c>clinuv</c>.</param>
/// <param name="PlanCode">The plan — <c>pro</c>.</param>
/// <param name="Entitlements">
/// What the plan grants, as the JSON object the product defined in the Account's catalogue — limits,
/// modules, seats. An empty object when the plan defines none.
/// </param>
/// <remarks>
/// The Account issues it when the subscription is active, in trial, or past due while Stripe retries
/// the renewal, and reads it again on every token renewal, so a plan bought or lapsed reaches the
/// product with the next token.
/// </remarks>
public record NuvToolsAccountPlan(string ProductCode, string PlanCode, JsonElement Entitlements);

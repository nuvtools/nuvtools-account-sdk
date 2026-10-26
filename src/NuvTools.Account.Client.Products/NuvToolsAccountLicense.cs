namespace NuvTools.Account.Client.Products;

/// <summary>The licence behind an answer of the gate.</summary>
/// <param name="Code">The licence type's code — <c>professional</c>.</param>
/// <param name="Billed">Whether its seat is paid for.</param>
public record NuvToolsAccountLicense(string Code, bool Billed);

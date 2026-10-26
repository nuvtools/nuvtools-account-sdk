namespace NuvTools.Account.Client.AspNetCore;

/// <summary>A licence type the product sells, as it publishes it.</summary>
/// <param name="Code">Its handle — <c>professional</c>; also how a plan names its seat price.</param>
/// <param name="Name">Its name in English.</param>
/// <param name="NamePtBr">Its name in Portuguese.</param>
/// <param name="IsBilled">Each assigned seat is paid for.</param>
public record NuvToolsAccountLicenseType(string Code, string Name, string? NamePtBr = null, bool IsBilled = false);

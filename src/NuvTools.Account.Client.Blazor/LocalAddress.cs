namespace NuvTools.Account.Client.Blazor;

/// <summary>Keeps a return address on this app.</summary>
internal static class LocalAddress
{
    /// <summary>
    /// The address when it is a path on this app — starts with one slash, not two, and no
    /// backslash — and <see langword="null"/> otherwise, so a crafted link cannot send a person who
    /// just signed in to another site.
    /// </summary>
    public static string? OrNull(string? address) =>
        !string.IsNullOrEmpty(address)
        && address[0] == '/'
        && (address.Length == 1 || (address[1] != '/' && address[1] != '\\'))
        && !address.Contains('\\', StringComparison.Ordinal)
            ? address
            : null;
}

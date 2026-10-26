namespace NuvTools.Account.Client.AspNetCore.UnitTests;

/// <summary>A clock a test moves by hand.</summary>
internal sealed class ManualClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}

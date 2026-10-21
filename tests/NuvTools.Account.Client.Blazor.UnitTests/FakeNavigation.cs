using Microsoft.AspNetCore.Components;

namespace NuvTools.Account.Client.Blazor.UnitTests;

/// <summary>A navigation manager that records where the app was sent instead of going there.</summary>
internal sealed class FakeNavigation : NavigationManager
{
    public const string Base = "https://product.nuvtools.test/";

    public FakeNavigation() => Initialize(Base, Base);

    public string? LastAddress { get; private set; }

    protected override void NavigateToCore(string uri, NavigationOptions options) => LastAddress = uri;
}

using NuvTools.AspNetCore.Blazor.JSInterop;
using System.Text.Json;

namespace NuvTools.Account.Client.Blazor.UnitTests;

/// <summary>Session storage in memory, serialized as the browser's would be.</summary>
internal sealed class FakeSessionStorage : ISessionStorageService
{
    public Dictionary<string, string> Items { get; } = [];

    public ValueTask<T?> GetItemAsync<T>(string key) =>
        ValueTask.FromResult(Items.TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json) : default);

    public ValueTask<string?> GetItemAsStringAsync(string key) =>
        ValueTask.FromResult(Items.TryGetValue(key, out var value) ? value : null);

    public ValueTask SetItemAsync<T>(string key, T value)
    {
        Items[key] = JsonSerializer.Serialize(value);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetItemAsStringAsync(string key, string value)
    {
        Items[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveItemAsync(string key)
    {
        Items.Remove(key);
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync()
    {
        Items.Clear();
        return ValueTask.CompletedTask;
    }

    public ValueTask<int> LengthAsync() => ValueTask.FromResult(Items.Count);

    public ValueTask<string?> KeyAsync(int index) => ValueTask.FromResult<string?>(Items.Keys.ElementAtOrDefault(index));

    public ValueTask<bool> ContainKeyAsync(string key) => ValueTask.FromResult(Items.ContainsKey(key));
}

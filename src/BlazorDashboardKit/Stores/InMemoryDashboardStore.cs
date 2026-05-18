using System.Collections.Concurrent;
using System.Text.Json;
using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;

namespace BlazorDashboardKit.Stores;

public sealed class InMemoryDashboardStore : IDashboardStore
{
    private readonly ConcurrentDictionary<string, string> _data = new();

    public Task<DashboardCollection?> LoadAsync(string ownerKey, CancellationToken ct = default)
        => Task.FromResult(_data.TryGetValue(ownerKey, out var json)
            ? JsonSerializer.Deserialize<DashboardCollection>(json)
            : null);

    public Task SaveAsync(string ownerKey, DashboardCollection collection, CancellationToken ct = default)
    {
        _data[ownerKey] = JsonSerializer.Serialize(collection);
        return Task.CompletedTask;
    }
}

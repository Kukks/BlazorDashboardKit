using BlazorDashboardKit.Models;

namespace BlazorDashboardKit.Abstractions;

public interface IDashboardStore
{
    Task<DashboardCollection?> LoadAsync(string ownerKey, CancellationToken ct = default);
    Task SaveAsync(string ownerKey, DashboardCollection collection, CancellationToken ct = default);
}

using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;

namespace BlazorDashboardKit.Services;

internal sealed class DashboardService
{
    private readonly IDashboardStore _store;
    public DashboardService(IDashboardStore store) => _store = store;

    public async Task<(DashboardCollection collection, DashboardDefinition active)> ResolveAsync(
        string ownerKey, CancellationToken ct)
    {
        var collection = await _store.LoadAsync(ownerKey, ct) ?? new DashboardCollection();
        var active = collection.Dashboards.FirstOrDefault(d => d.Id == collection.ActiveDashboardId)
                     ?? collection.Dashboards.FirstOrDefault(d => d.IsDefault)
                     ?? collection.Dashboards.FirstOrDefault();
        if (active is null)
        {
            active = new DashboardDefinition { Name = "Dashboard", IsDefault = true };
            collection.Dashboards.Add(active);
            collection.ActiveDashboardId = active.Id;
        }
        return (collection, active);
    }

    public async Task SaveAsync(string ownerKey, DashboardCollection collection, CancellationToken ct)
    {
        var fresh = await _store.LoadAsync(ownerKey, ct);
        if (fresh is not null)
            collection.ActiveDashboardId ??= fresh.ActiveDashboardId;
        await _store.SaveAsync(ownerKey, collection, ct);
    }
}

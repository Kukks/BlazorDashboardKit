using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;
using BlazorDashboardKit.Services;
using BlazorDashboardKit.Stores;
using Xunit;

namespace BlazorDashboardKit.Tests.Services;

public class DashboardServiceTests
{
    private static DashboardService With(DashboardCollection? seeded, out InMemoryDashboardStore store)
    {
        store = new InMemoryDashboardStore();
        if (seeded is not null)
            store.SaveAsync("owner", seeded).GetAwaiter().GetResult();
        return new DashboardService(store);
    }

    [Fact]
    public async Task ResolveAsync_Creates_A_Default_Dashboard_When_Store_Is_Empty()
    {
        var svc = With(null, out _);

        var (collection, active) = await svc.ResolveAsync("owner", default);

        Assert.Single(collection.Dashboards);
        Assert.Same(collection.Dashboards[0], active);
        Assert.True(active.IsDefault);
        Assert.Equal(active.Id, collection.ActiveDashboardId);
    }

    [Fact]
    public async Task ResolveAsync_Selects_The_Active_Dashboard_By_Id()
    {
        var a = new DashboardDefinition { Id = "a", Name = "A" };
        var b = new DashboardDefinition { Id = "b", Name = "B" };
        var svc = With(new DashboardCollection { ActiveDashboardId = "b", Dashboards = { a, b } }, out _);

        var (_, active) = await svc.ResolveAsync("owner", default);

        Assert.Equal("b", active.Id);
    }

    [Fact]
    public async Task ResolveAsync_Falls_Back_To_IsDefault_Then_First()
    {
        var first = new DashboardDefinition { Id = "1", Name = "First" };
        var def = new DashboardDefinition { Id = "2", Name = "Def", IsDefault = true };

        var byDefault = With(new DashboardCollection { ActiveDashboardId = "missing", Dashboards = { first, def } }, out _);
        Assert.Equal("2", (await byDefault.ResolveAsync("owner", default)).active.Id);

        var byFirst = With(new DashboardCollection { Dashboards = { first } }, out _);
        Assert.Equal("1", (await byFirst.ResolveAsync("owner", default)).active.Id);
    }

    [Fact]
    public async Task SaveAsync_Preserves_An_ActiveDashboardId_Set_By_Another_Writer()
    {
        // Another writer persisted an ActiveDashboardId after we resolved; our
        // collection's is null, so the save must keep theirs (not blank it).
        var store = new InMemoryDashboardStore();
        await store.SaveAsync("owner", new DashboardCollection { ActiveDashboardId = "other" });
        var svc = new DashboardService(store);

        var mine = new DashboardCollection
        {
            ActiveDashboardId = null,
            Dashboards = { new DashboardDefinition { Id = "x" } }
        };
        await svc.SaveAsync("owner", mine, default);

        var reloaded = await store.LoadAsync("owner");
        Assert.Equal("other", reloaded!.ActiveDashboardId);
    }
}

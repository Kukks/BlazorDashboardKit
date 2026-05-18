using BlazorDashboardKit.Models;
using BlazorDashboardKit.Stores;
using Xunit;

namespace BlazorDashboardKit.Tests.Stores;

public class InMemoryDashboardStoreTests
{
    [Fact]
    public async Task Save_Then_Load_Returns_DeepCopy_PerOwner()
    {
        var s = new InMemoryDashboardStore();
        Assert.Null(await s.LoadAsync("owner-a"));
        var c = new DashboardCollection { ActiveDashboardId = "x" };
        await s.SaveAsync("owner-a", c);
        c.ActiveDashboardId = "mutated-after-save";
        var loaded = await s.LoadAsync("owner-a");
        Assert.Equal("x", loaded!.ActiveDashboardId);
        Assert.Null(await s.LoadAsync("owner-b"));
    }
}

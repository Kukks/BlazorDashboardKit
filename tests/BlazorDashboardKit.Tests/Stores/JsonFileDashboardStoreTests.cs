using BlazorDashboardKit.Models;
using BlazorDashboardKit.Stores;
using Xunit;

namespace BlazorDashboardKit.Tests.Stores;

public class JsonFileDashboardStoreTests
{
    [Fact]
    public async Task Save_Then_Load_AcrossInstances_Persists()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bdk-" + Guid.NewGuid());
        try
        {
            var a = new JsonFileDashboardStore(dir);
            await a.SaveAsync("user/../weird key", new DashboardCollection { ActiveDashboardId = "z" });
            var b = new JsonFileDashboardStore(dir);
            var loaded = await b.LoadAsync("user/../weird key");
            Assert.Equal("z", loaded!.ActiveDashboardId);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}

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

    [Fact]
    public async Task Corrupt_File_Does_Not_Crash_Load_And_Is_Quarantined()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bdk-" + Guid.NewGuid());
        try
        {
            var store = new JsonFileDashboardStore(dir);
            await store.SaveAsync("owner", new DashboardCollection { ActiveDashboardId = "ok" });

            // Corrupt the persisted file (partial write / manual edit / disk issue).
            var file = Directory.GetFiles(dir, "*.json").Single();
            await File.WriteAllTextAsync(file, "{ this is not json");

            // Must recover, not throw, and the bad data must be preserved.
            var loaded = await store.LoadAsync("owner");
            Assert.Null(loaded);
            Assert.False(File.Exists(file));
            Assert.NotEmpty(Directory.GetFiles(dir, "*.corrupt*"));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}

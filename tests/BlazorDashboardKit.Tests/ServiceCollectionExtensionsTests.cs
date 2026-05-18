using BlazorDashboardKit;
using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;
using BlazorDashboardKit.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorDashboardKit.Tests;

public class ServiceCollectionExtensionsTests
{
    private sealed class DummyWidget : ComponentBase { }

    [Fact]
    public void AddBlazorDashboard_Registers_Defaults_And_Widgets()
    {
        var sp = new ServiceCollection()
            .AddBlazorDashboard()
            .AddDashboardWidget<DummyWidget>(new WidgetDescriptor { Type = "Dummy", Name = "Dummy" })
            .BuildServiceProvider();

        Assert.NotNull(sp.GetRequiredService<IDashboardStore>());
        Assert.NotNull(sp.GetRequiredService<IWidgetAccessControl>());
        Assert.Single(sp.GetServices<WidgetDescriptor>());
        Assert.NotNull(sp.GetRequiredService<WidgetRegistry>());
    }

    [Fact]
    public void AddBlazorDashboard_UseJsonFileStore_Overrides_Default_Store()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bdk-di-" + Guid.NewGuid());
        var sp = new ServiceCollection()
            .AddBlazorDashboard(o => o.UseJsonFileStore(dir))
            .BuildServiceProvider();
        Assert.IsType<BlazorDashboardKit.Stores.JsonFileDashboardStore>(sp.GetRequiredService<IDashboardStore>());
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }
}

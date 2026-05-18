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

    private sealed class CustomStore : IDashboardStore
    {
        public Task<DashboardCollection?> LoadAsync(string ownerKey, CancellationToken ct = default)
            => Task.FromResult<DashboardCollection?>(null);
        public Task SaveAsync(string ownerKey, DashboardCollection collection, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class DenyAll : IWidgetAccessControl
    {
        public Task<bool> IsAllowedAsync(WidgetDescriptor d, System.Security.Claims.ClaimsPrincipal? u, CancellationToken ct = default)
            => Task.FromResult(false);
    }

    [Fact]
    public void Consumer_Registered_Store_And_AccessControl_Win_Over_Defaults()
    {
        // Documented contract: register before AddBlazorDashboard() and TryAdd
        // means your implementation is used, not the kit's defaults.
        var sp = new ServiceCollection()
            .AddSingleton<IDashboardStore, CustomStore>()
            .AddSingleton<IWidgetAccessControl, DenyAll>()
            .AddBlazorDashboard()
            .BuildServiceProvider();

        Assert.IsType<CustomStore>(sp.GetRequiredService<IDashboardStore>());
        Assert.IsType<DenyAll>(sp.GetRequiredService<IWidgetAccessControl>());
    }

    [Fact]
    public void AddDashboardWidget_Sets_ComponentType_And_Feeds_The_Registry()
    {
        var sp = new ServiceCollection()
            .AddBlazorDashboard()
            .AddDashboardWidget<DummyWidget>(new WidgetDescriptor { Type = "Dummy", Name = "Dummy" })
            .BuildServiceProvider();

        var registry = sp.GetRequiredService<WidgetRegistry>();
        var d = registry.GetDescriptor("Dummy");
        Assert.NotNull(d);
        Assert.Equal(typeof(DummyWidget), d!.ComponentType);
    }
}

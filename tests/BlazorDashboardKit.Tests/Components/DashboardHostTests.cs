using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;
using BlazorDashboardKit.Services;
using BlazorDashboardKit.Stores;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BlazorDashboardKit.Tests.Components;

public class DashboardHostTests : TestContext
{
    [Fact]
    public async Task Loads_Persisted_Dashboard_And_Flags_Unknown_Widget()
    {
        var store = new InMemoryDashboardStore();
        var c = new DashboardCollection { ActiveDashboardId = "d1" };
        c.Dashboards.Add(new DashboardDefinition { Id = "d1",
            Widgets = { new WidgetPlacement { WidgetType = "Ghost" } } });
        await store.SaveAsync("owner-1", c);

        // Loose JS so the host's interactive grid-init (bunit invokes
        // OnAfterRenderAsync, which flips the net8 interactivity gate) resolves to
        // inert no-ops instead of throwing. We never call JSInterop.SetupModule,
        // so bunit's module-mock machinery (the documented VSTest-host hang) is
        // never engaged; the assertion below only inspects the synchronous static
        // markup, which renders before OnAfterRenderAsync.
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IDashboardStore>(store);
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(Array.Empty<WidgetDescriptor>()));
        Services.AddScoped<DashboardService>();
        Services.AddScoped<DashboardJsInterop>(_ => new DashboardJsInterop(JSInterop.JSRuntime));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1"));
        cut.WaitForState(() => cut.Markup.Contains("widget-unavailable"), TimeSpan.FromSeconds(5));
        Assert.Contains("widget-unavailable", cut.Markup);
    }

    [Fact]
    public void Empty_OwnerKey_Renders_Inert_Container_And_Never_Touches_Store()
    {
        // An empty OwnerKey means "no owner": the host must render the inert
        // no-owner branch without ever resolving/saving against the store.
        // ThrowingStore turns any store access into a hard test failure.
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IDashboardStore>(new ThrowingStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(Array.Empty<WidgetDescriptor>()));
        Services.AddScoped<DashboardService>();
        Services.AddScoped<DashboardJsInterop>(_ => new DashboardJsInterop(JSInterop.JSRuntime));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, ""));

        Assert.Contains("dashboard-empty-container", cut.Markup);
        Assert.DoesNotContain("dashboard-header", cut.Markup);
    }

    /// <summary>
    /// Store whose every operation throws, so a single store access fails the test.
    /// </summary>
    private sealed class ThrowingStore : IDashboardStore
    {
        public Task<DashboardCollection?> LoadAsync(string ownerKey, CancellationToken ct = default)
            => throw new InvalidOperationException("Store must not be accessed for an empty OwnerKey.");

        public Task SaveAsync(string ownerKey, DashboardCollection collection, CancellationToken ct = default)
            => throw new InvalidOperationException("Store must not be accessed for an empty OwnerKey.");
    }
}

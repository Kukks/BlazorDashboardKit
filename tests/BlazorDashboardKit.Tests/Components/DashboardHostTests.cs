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
}

using AngleSharp.Dom;
using BlazorDashboardKit.Abstractions;
using BlazorDashboardKit.Models;
using BlazorDashboardKit.Services;
using BlazorDashboardKit.Stores;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
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
    public void Actions_Menu_Toggles_Open_Without_Bootstrap_Js()
    {
        // The Actions dropdown must open via Blazor state, not Bootstrap's JS
        // bundle (which the kit deliberately does not ship or require).
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(Array.Empty<WidgetDescriptor>()));
        Services.AddScoped<DashboardService>();
        Services.AddScoped<DashboardJsInterop>(_ => new DashboardJsInterop(JSInterop.JSRuntime));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true));

        cut.WaitForState(
            () => cut.FindAll("button.btn-outline-secondary.dropdown-toggle").Count == 1,
            TimeSpan.FromSeconds(5));

        IElement ActionsMenu() => cut
            .Find("button.btn-outline-secondary.dropdown-toggle")
            .ParentElement!
            .QuerySelector(".dropdown-menu")!;

        Assert.DoesNotContain("show", ActionsMenu().ClassList);

        cut.Find("button.btn-outline-secondary.dropdown-toggle").Click();
        Assert.Contains("show", ActionsMenu().ClassList);

        cut.Find("button.btn-outline-secondary.dropdown-toggle").Click();
        Assert.DoesNotContain("show", ActionsMenu().ClassList);
    }

    [Fact]
    public void Actions_Menu_Closes_After_Choosing_Export()
    {
        // Parity with the widget picker: choosing an item dismisses the menu,
        // the way Bootstrap's JS used to.
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(Array.Empty<WidgetDescriptor>()));
        Services.AddScoped<DashboardService>();
        Services.AddScoped<DashboardJsInterop>(_ => new DashboardJsInterop(JSInterop.JSRuntime));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true));

        cut.WaitForState(
            () => cut.FindAll("button.btn-outline-secondary.dropdown-toggle").Count == 1,
            TimeSpan.FromSeconds(5));

        IElement ActionsMenu() => cut
            .Find("button.btn-outline-secondary.dropdown-toggle")
            .ParentElement!
            .QuerySelector(".dropdown-menu")!;

        cut.Find("button.btn-outline-secondary.dropdown-toggle").Click();
        Assert.Contains("show", ActionsMenu().ClassList);

        cut.Find(".dropdown-menu .dropdown-item").Click(); // "Export"
        Assert.DoesNotContain("show", ActionsMenu().ClassList);
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

    [Fact]
    public void Grid_Is_Reinitialized_After_Adding_A_Widget()
    {
        // Repro: AddWidget -> ResetGrid() destroys the GridStack instance and sets
        // _gridInitialized=false, expecting the next render to re-init. The sole
        // InitGrid call site is gated on OnAfterRenderAsync's `firstRender`, which
        // is true exactly once. So a widget added after first render is never
        // adopted by GridStack and collapses to 0x0 at position 0,0. The grid
        // MUST re-initialize after the widget set changes.
        var js = new RecordingJsRuntime();
        var descriptor = new WidgetDescriptor
        {
            Type = "Test",
            Name = "Test Widget",
            Category = "Demo",
            ComponentType = typeof(TestWidget)
        };

        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(new[] { descriptor }));
        Services.AddScoped<DashboardService>();
        Services.AddScoped(_ => new DashboardJsInterop(js));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true));

        cut.WaitForState(() => js.InitGridCalls == 1, TimeSpan.FromSeconds(5));

        cut.Find("button.btn-outline-primary.dropdown-toggle").Click(); // open picker
        cut.Find("button.dropdown-item.small").Click();                  // add the widget

        cut.WaitForState(() => js.InitGridCalls >= 2, TimeSpan.FromSeconds(5));
        Assert.Equal(2, js.InitGridCalls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Widget_Debug_Label_Is_Opt_In(bool showDebugInfo, bool expectLabel)
    {
        var js = new RecordingJsRuntime();
        var descriptor = new WidgetDescriptor
        {
            Type = "Test", Name = "Test Widget", Category = "Demo",
            ComponentType = typeof(TestWidget)
        };

        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(new[] { descriptor }));
        Services.AddScoped<DashboardService>();
        Services.AddScoped(_ => new DashboardJsInterop(js));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true)
            .Add(x => x.ShowDebugInfo, showDebugInfo));

        cut.WaitForState(() => js.InitGridCalls == 1, TimeSpan.FromSeconds(5));
        cut.Find("button.btn-outline-primary.dropdown-toggle").Click();
        cut.Find("button.dropdown-item.small").Click();
        cut.WaitForState(() => cut.Markup.Contains("test-widget-body"), TimeSpan.FromSeconds(5));

        Assert.Equal(expectLabel, cut.Markup.Contains("widget-debug-label"));
    }

    [Fact]
    public void Duplicating_A_Widget_Adds_An_Independent_Copy()
    {
        var js = new RecordingJsRuntime();
        var descriptor = new WidgetDescriptor
        {
            Type = "Test", Name = "Test Widget", Category = "Demo",
            ComponentType = typeof(TestWidget)
        };
        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(new[] { descriptor }));
        Services.AddScoped<DashboardService>();
        Services.AddScoped(_ => new DashboardJsInterop(js));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true));

        cut.WaitForState(() => js.InitGridCalls == 1, TimeSpan.FromSeconds(5));
        cut.Find("button.btn-outline-primary.dropdown-toggle").Click();
        cut.Find("button.dropdown-item.small").Click();
        cut.WaitForState(() => cut.FindAll(".grid-stack-item").Count == 1, TimeSpan.FromSeconds(5));

        cut.Find("button[title='Duplicate widget']").Click();
        cut.WaitForState(() => cut.FindAll(".grid-stack-item").Count == 2, TimeSpan.FromSeconds(5));

        var ids = cut.FindAll(".grid-stack-item").Select(e => e.GetAttribute("gs-id")).ToList();
        Assert.Equal(2, ids.Count);
        Assert.Equal(2, ids.Distinct().Count());                 // distinct placement ids
        Assert.Equal(2, cut.FindAll(".test-widget-body").Count);  // both render the same widget
    }

    [Fact]
    public void Locking_A_Widget_Emits_GridStack_Lock_Attributes()
    {
        var js = new RecordingJsRuntime();
        var descriptor = new WidgetDescriptor
        {
            Type = "Test", Name = "Test Widget", Category = "Demo",
            ComponentType = typeof(TestWidget)
        };
        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(new[] { descriptor }));
        Services.AddScoped<DashboardService>();
        Services.AddScoped(_ => new DashboardJsInterop(js));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true));

        cut.WaitForState(() => js.InitGridCalls == 1, TimeSpan.FromSeconds(5));
        cut.Find("button.btn-outline-primary.dropdown-toggle").Click();
        cut.Find("button.dropdown-item.small").Click();
        cut.WaitForState(() => cut.FindAll(".grid-stack-item").Count == 1, TimeSpan.FromSeconds(5));

        Assert.Null(cut.Find(".grid-stack-item").GetAttribute("gs-locked"));

        cut.Find("button[title='Lock widget']").Click();
        cut.WaitForState(
            () => cut.Find(".grid-stack-item").GetAttribute("gs-locked") == "true",
            TimeSpan.FromSeconds(5));

        var item = cut.Find(".grid-stack-item");
        Assert.Equal("true", item.GetAttribute("gs-no-move"));
        Assert.Equal("true", item.GetAttribute("gs-no-resize"));
        Assert.NotEmpty(cut.FindAll("button[title='Unlock widget']")); // toggled
    }

    [Fact]
    public void Custom_GridOptions_Are_Forwarded_To_The_Grid_Init()
    {
        var js = new RecordingJsRuntime();
        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(Array.Empty<WidgetDescriptor>()));
        Services.AddScoped<DashboardService>();
        Services.AddScoped(_ => new DashboardJsInterop(js));

        var opts = new DashboardGridOptions { Columns = 6, CellHeight = 100, Margin = 4, MobileColumns = 2 };
        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true)
            .Add(x => x.GridOptions, opts));

        cut.WaitForState(() => js.InitGridCalls == 1, TimeSpan.FromSeconds(5));

        Assert.NotNull(js.LastGridOptions);
        Assert.Equal(6, js.LastGridOptions!.Columns);
        Assert.Equal(100, js.LastGridOptions.CellHeight);
        Assert.Equal(4, js.LastGridOptions.Margin);
        Assert.Equal(2, js.LastGridOptions.MobileColumns);
    }

    [Fact]
    public void Default_GridOptions_Are_Sent_When_None_Provided()
    {
        var js = new RecordingJsRuntime();
        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(Array.Empty<WidgetDescriptor>()));
        Services.AddScoped<DashboardService>();
        Services.AddScoped(_ => new DashboardJsInterop(js));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true));

        cut.WaitForState(() => js.InitGridCalls == 1, TimeSpan.FromSeconds(5));

        Assert.NotNull(js.LastGridOptions);
        Assert.Equal(12, js.LastGridOptions!.Columns);   // kit default
        Assert.Equal(146, js.LastGridOptions.CellHeight);
    }

    [Fact]
    public void Widget_Size_Constraints_From_Descriptor_Reach_The_Grid()
    {
        // The descriptor declares Min/Max column/row sizes; they must be emitted
        // as GridStack gs-min/max attributes or resize is unconstrained.
        var js = new RecordingJsRuntime();
        var descriptor = new WidgetDescriptor
        {
            Type = "Test", Name = "Test Widget", Category = "Demo",
            ComponentType = typeof(TestWidget),
            MinColumnSize = 2, MaxColumnSize = 8,
            MinRowSpan = 1, MaxRowSpan = 5
        };

        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(new[] { descriptor }));
        Services.AddScoped<DashboardService>();
        Services.AddScoped(_ => new DashboardJsInterop(js));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true));

        cut.WaitForState(() => js.InitGridCalls == 1, TimeSpan.FromSeconds(5));
        cut.Find("button.btn-outline-primary.dropdown-toggle").Click();
        cut.Find("button.dropdown-item.small").Click();
        cut.WaitForState(() => cut.FindAll(".grid-stack-item").Count == 1, TimeSpan.FromSeconds(5));

        var item = cut.Find(".grid-stack-item");
        Assert.Equal("2", item.GetAttribute("gs-min-w"));
        Assert.Equal("8", item.GetAttribute("gs-max-w"));
        Assert.Equal("1", item.GetAttribute("gs-min-h"));
        Assert.Equal("5", item.GetAttribute("gs-max-h"));
    }

    [Fact]
    public void Static_Fallback_Class_Is_Cleared_Once_Grid_Is_Live()
    {
        // The container carries `bdk-grid-static` (CSS fallback layout) until
        // GridStack is initialized. Once live the class MUST be gone, otherwise
        // its !important rules override GridStack's positioning.
        var js = new RecordingJsRuntime();
        var descriptor = new WidgetDescriptor
        {
            Type = "Test", Name = "Test Widget", Category = "Demo",
            ComponentType = typeof(TestWidget)
        };

        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(new[] { descriptor }));
        Services.AddScoped<DashboardService>();
        Services.AddScoped(_ => new DashboardJsInterop(js));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true));

        cut.WaitForState(() => js.InitGridCalls == 1, TimeSpan.FromSeconds(5));
        cut.WaitForState(
            () => !cut.Find(".grid-stack").ClassList.Contains("bdk-grid-static"),
            TimeSpan.FromSeconds(5));

        Assert.DoesNotContain("bdk-grid-static", cut.Find(".grid-stack").ClassList);
    }

    [Fact]
    public void Grid_Is_Reinitialized_After_Removing_A_Widget()
    {
        // AddWidget ends with ResetGrid() so the grid rebuilds; RemoveWidget must
        // do the same. Otherwise GridStack keeps a stale internal model that still
        // references the removed DOM node and the remaining widgets are not relaid.
        var js = new RecordingJsRuntime();
        var descriptor = new WidgetDescriptor
        {
            Type = "Test",
            Name = "Test Widget",
            Category = "Demo",
            ComponentType = typeof(TestWidget)
        };

        Services.AddSingleton<IDashboardStore>(new InMemoryDashboardStore());
        Services.AddSingleton<IWidgetAccessControl, AllowAllWidgetAccessControl>();
        Services.AddSingleton(new WidgetRegistry(new[] { descriptor }));
        Services.AddScoped<DashboardService>();
        Services.AddScoped(_ => new DashboardJsInterop(js));

        var cut = RenderComponent<BlazorDashboardKit.Components.DashboardHost>(p => p
            .Add(x => x.OwnerKey, "owner-1")
            .Add(x => x.EditMode, true));

        cut.WaitForState(() => js.InitGridCalls == 1, TimeSpan.FromSeconds(5));

        cut.Find("button.btn-outline-primary.dropdown-toggle").Click(); // open picker
        cut.Find("button.dropdown-item.small").Click();                  // add widget
        cut.WaitForState(() => js.InitGridCalls == 2, TimeSpan.FromSeconds(5));

        cut.Find("button.widget-control-btn--danger").Click();           // remove widget
        cut.WaitForState(() => js.InitGridCalls >= 3, TimeSpan.FromSeconds(5));
        Assert.Equal(3, js.InitGridCalls);
    }

    /// <summary>
    /// Minimal <see cref="IJSRuntime"/> that resolves the interop ESM module to a
    /// recording stand-in, letting the test count how many times the host asks the
    /// module to run <c>initGrid</c> without engaging bunit's module-mock machinery.
    /// </summary>
    private sealed class RecordingJsRuntime : IJSRuntime
    {
        private readonly RecordingModule _module = new();
        public int InitGridCalls => _module.InitGridCalls;
        public DashboardGridOptions? LastGridOptions => _module.LastGridOptions;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            // DashboardJsInterop.InitGridAsync does: _js.InvokeAsync<IJSObjectReference>("import", ...)
            if (identifier == "import")
                return new ValueTask<TValue>((TValue)(object)_module);
            return new ValueTask<TValue>(default(TValue)!);
        }

        private sealed class RecordingModule : IJSObjectReference
        {
            public int InitGridCalls;
            public DashboardGridOptions? LastGridOptions;

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
                => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

            public ValueTask<TValue> InvokeAsync<TValue>(
                string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                if (identifier == "initGrid")
                {
                    InitGridCalls++;
                    // initGrid args: [containerId, dotNetRef, editMode, DashboardGridOptions]
                    LastGridOptions = args?.OfType<DashboardGridOptions>().FirstOrDefault();
                }
                return new ValueTask<TValue>(default(TValue)!);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
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

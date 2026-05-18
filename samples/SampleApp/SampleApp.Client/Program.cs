using BlazorDashboardKit;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SampleApp.Client.Widgets;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Dashboard kit (WebAssembly side). These registrations are required so that
// pages rendered with @rendermode InteractiveWebAssembly (e.g. /wasm) can
// resolve DashboardService / WidgetRegistry inside the browser. They must
// mirror the server-side registrations in SampleApp/Program.cs.
//
// Demo limitation: the default in-memory store lives in the WASM process and is
// separate from the server's in-memory store, so a layout edited on /wasm is
// not shared with the server-rendered pages (and vice versa). A real host would
// register a shared IDashboardStore identically on both sides.
builder.Services.AddBlazorDashboard()
    .AddDashboardWidget<NotesWidget>(NotesWidget.Descriptor)
    .AddDashboardWidget<ClockWidget>(ClockWidget.Descriptor)
    .AddDashboardWidget<StatsCardWidget>(StatsCardWidget.Descriptor);

await builder.Build().RunAsync();

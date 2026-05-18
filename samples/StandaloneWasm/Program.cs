using BlazorDashboardKit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StandaloneWasm;
using StandaloneWasm.Widgets;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// One line wires the dashboard + an in-memory store + allow-all policy.
// Swap in UseJsonFileStore / a custom IDashboardStore for real persistence.
builder.Services.AddBlazorDashboard()
    .AddDashboardWidget<CounterWidget>(CounterWidget.Descriptor)
    .AddDashboardWidget<MarkdownNoteWidget>(MarkdownNoteWidget.Descriptor);

await builder.Build().RunAsync();

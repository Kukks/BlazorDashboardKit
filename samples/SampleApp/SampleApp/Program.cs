using BlazorDashboardKit;
using SampleApp.Client.Pages;
using SampleApp.Client.Widgets;
using SampleApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Dashboard kit (server side). In a Blazor Web App "Auto" template the same
// services must be registered in BOTH this server project and the .Client
// project's Program.cs: server-rendered (and prerendered) instances resolve
// these, while interactive-WebAssembly instances resolve the copies registered
// client-side. The default in-memory store is per-process, so a dashboard saved
// on the server circuit is NOT visible to a WASM-rendered page in the same
// browser (different process). That is acceptable for this demo; a real host
// would back AddBlazorDashboard with a shared store (e.g. UseJsonFileStore or a
// custom IDashboardStore) registered identically on both sides.
builder.Services.AddBlazorDashboard()
    .AddDashboardWidget<NotesWidget>(NotesWidget.Descriptor)
    .AddDashboardWidget<ClockWidget>(ClockWidget.Descriptor)
    .AddDashboardWidget<StatsCardWidget>(StatsCardWidget.Descriptor);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SampleApp.Client._Imports).Assembly);

app.Run();

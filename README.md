# BlazorDashboardKit

A reusable, host-agnostic Blazor library for building customizable widget dashboards: a draggable/resizable grid of pluggable widgets, with the persistence and access policy supplied by you. It runs in Blazor Server, WebAssembly, and static SSR — the host component is render-mode-safe and touches no JS until it is interactive. The library only defines the dashboard surface and widget contract; it never assumes a database, a user model, or an application. Licensed MIT.

**Status:** pre-release

## Install

```
dotnet add package BlazorDashboardKit
```

Not yet published to nuget.org (pre-release). Until it is, reference the locally built `BlazorDashboardKit.0.1.0.nupkg` (e.g. `dotnet pack src/BlazorDashboardKit -c Release -o ./artifacts` then `dotnet add package BlazorDashboardKit --source ./artifacts --version 0.1.0`) or add a project reference to `src/BlazorDashboardKit`.

## Wire up DI

In `Program.cs`:

```csharp
using BlazorDashboardKit;

builder.Services.AddBlazorDashboard()
    .AddDashboardWidget<MyWidget>(MyWidget.Descriptor);
```

`AddBlazorDashboard()` with no arguments registers an in-memory store (per-process, not persisted) and an allow-all access policy. To persist dashboards as JSON files on disk, supply the options callback:

```csharp
builder.Services.AddBlazorDashboard(o => o.UseJsonFileStore("/var/data/dashboards"));
```

`AddDashboardWidget<TComponent>(descriptor)` registers a widget type with its descriptor; call it once per widget. Register the same services on every render target you use: in a Blazor Web App with WebAssembly/Auto interactivity, the server project and the `.Client` project each have their own DI container, so `AddBlazorDashboard()` (and your widgets) must be registered in both `Program.cs` files.

## Assets

Add these to the `<head>` of your host page (`App.razor`):

```html
<link rel="stylesheet" href="_content/BlazorDashboardKit/gridstack/gridstack.min.css" />
<link rel="stylesheet" href="_content/BlazorDashboardKit/dashboard.css" />
```

And this script before `blazor.web.js` (or `blazor.server.js` / `blazor.webassembly.js`) in the body:

```html
<script src="_content/BlazorDashboardKit/gridstack/gridstack-all.js"></script>
```

`dashboard-interop.js` is an ESM module that the library imports itself from `_content/BlazorDashboardKit/dashboard-interop.js` — do **not** add a script tag for it. The gridstack library above must be a global script because the interop module references `globalThis.GridStack`.

## Use the host

Add `@using BlazorDashboardKit.Components` to `_Imports.razor`, then drop the host into any page:

```razor
<DashboardHost OwnerKey="@ownerKey" EditMode="true" />
```

- **`OwnerKey`** (required) — an opaque, consumer-chosen string identifying whose dashboard this is. It is passed straight to your `IDashboardStore`; use a user id, tenant id, store id, or any key you control. A null/empty key renders an empty container and never touches the store.
- **`EditMode`** — start in edit mode (add/remove/drag/resize widgets, rename, import/export). Defaults to `false`.
- **`ReadOnly`** — when `true`, hides the "Edit" affordance entirely so the dashboard cannot be edited.
- Authentication, when relevant, flows in automatically via a cascading `Task<AuthenticationState>` if your app provides one (the standard `AuthorizeRouteView` / `CascadingAuthenticationState` setup). Without it, the authenticated user is treated as `null` and the allow-all policy still grants access.

## Write a widget

Derive from `BaseWidgetComponent<TConfig>` and expose a `public static readonly WidgetDescriptor Descriptor`:

```razor
@inherits BlazorDashboardKit.Components.BaseWidgetComponent<MyWidget.MyConfig>
@using BlazorDashboardKit.Models

<div class="card h-100"><div class="card-body">@(TypedConfig?.Title)</div></div>

@code {
    public class MyConfig { public string Title { get; set; } = "Hello"; }

    public static readonly WidgetDescriptor Descriptor = new()
    {
        Type = "MyWidget",
        Name = "My Widget",
        Description = "An example widget",
        Category = "Demo",
        DefaultColumnSize = 3,
        ConfigSchema = new()
        {
            ["Title"] = new ConfigFieldSchema { Label = "Title", FieldType = ConfigFieldType.Text }
        }
    };
}
```

`TypedConfig` is the deserialized `TConfig` for the current placement. `ConfigSchema` keys must match `TConfig` property names; each entry drives one field in the built-in config panel (`FieldType` is `Text`, `Textarea`, `Number`, `Select`, `Checkbox`, or `Hidden`; `Select` uses `Options`, `Number` honours `Min`/`Max`, `Textarea` honours `Rows`). Set `RequiresConfiguration = true` to force the config panel open before a newly added widget is saved.

## Custom persistence

Implement `IDashboardStore` and register it before `AddBlazorDashboard()` (it uses `TryAdd`, so your registration wins):

```csharp
public interface IDashboardStore
{
    Task<DashboardCollection?> LoadAsync(string ownerKey, CancellationToken ct = default);
    Task SaveAsync(string ownerKey, DashboardCollection collection, CancellationToken ct = default);
}
```

`ownerKey` is the value you passed to `DashboardHost.OwnerKey`. `LoadAsync` returns `null` when nothing is stored for that key.

## Custom access control

The default policy (`AllowAllWidgetAccessControl`) allows every widget. To gate widgets, implement `IWidgetAccessControl` and register it before `AddBlazorDashboard()`:

```csharp
public interface IWidgetAccessControl
{
    Task<bool> IsAllowedAsync(WidgetDescriptor descriptor, ClaimsPrincipal? user, CancellationToken ct = default);
}
```

It is called both to filter the widget picker and to gate each rendered widget. `user` is the cascaded authentication state's principal, or `null` when there is no auth context. `WidgetDescriptor.RequiredPermissions` is a `string[]` of opaque tokens that the library never interprets — they mean only what your `IWidgetAccessControl` decides they mean.

## Render modes

`DashboardHost` works in Blazor Server, WebAssembly, and static SSR. It is render-mode-safe: during static SSR and prerender it emits markup only and never invokes JavaScript; the grid is initialized lazily once the component reaches an interactive render. The in-memory store is per-process, so if you render the same dashboard under different interactivity locations (e.g. a server-prerendered page that becomes WebAssembly-interactive) back it with a shared store (`UseJsonFileStore` or a custom `IDashboardStore`) registered identically on every side.

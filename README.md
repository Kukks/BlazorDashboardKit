# BlazorDashboardKit

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Kukks/BlazorDashboardKit)

A reusable, host-agnostic Blazor library for building customizable widget dashboards: a draggable/resizable grid of pluggable widgets, with the persistence and access policy supplied by you. It runs in Blazor Server, WebAssembly, and static SSR — the host component is render-mode-safe and touches no JS until it is interactive. The library only defines the dashboard surface and widget contract; it never assumes a database, a user model, or an application. Licensed MIT.

**Status:** pre-release · **[Live demo + usage →](https://kukks.github.io/BlazorDashboardKit/)**

## Install

```
dotnet add package BlazorDashboardKit
```

Pre-release. Also published to GitHub Packages for the `Kukks/BlazorDashboardKit` repo. Or add a project reference to `src/BlazorDashboardKit`.

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

Nothing to add by hand. A Blazor JS initializer shipped in the package
(`BlazorDashboardKit.lib.module.js`, auto-discovered for the RCL) injects the
kit's stylesheet, the GridStack stylesheet, and the GridStack script into the
host document before the app starts. The ESM interop (`dashboard-interop.js`)
is imported by the library itself. Injection is idempotent: if your host
already references an asset (e.g. you want to pin a version or control order),
the initializer skips it.

If you prefer fully manual control, you can still add them yourself — the
initializer will then no-op:

```html
<link rel="stylesheet" href="_content/BlazorDashboardKit/gridstack/gridstack.min.css" />
<link rel="stylesheet" href="_content/BlazorDashboardKit/dashboard.css" />
<script src="_content/BlazorDashboardKit/gridstack/gridstack-all.js"></script>
```

### Theming

The kit ships `--bdk-*` CSS custom properties with sensible standalone
defaults; each also falls back to the matching BTCPay Server variable, so the
kit adopts a BTCPay theme automatically. It also **auto-switches to a dark
palette** under `prefers-color-scheme: dark`. Override any token on an ancestor
(zero-specificity `:where()` selectors mean your values always win), e.g.

```css
:root {
    --bdk-surface: #0b1020;
    --bdk-text: #e8eaf0;
    --bdk-primary: #6c8cff;
    --bdk-radius: 0.75rem;
}
```

Common tokens: `--bdk-surface`, `--bdk-surface-muted`, `--bdk-text`,
`--bdk-text-muted`, `--bdk-border`, `--bdk-primary`, `--bdk-danger`,
`--bdk-radius`, `--bdk-shadow`, `--bdk-edit-header-height`,
`--bdk-fallback-item-min-height`.

## Use the host

Add `@using BlazorDashboardKit.Components` to `_Imports.razor`, then drop the host into any page:

```razor
<DashboardHost OwnerKey="@ownerKey" EditMode="true" />
```

- **`OwnerKey`** (required) — an opaque, consumer-chosen string identifying whose dashboard this is. It is passed straight to your `IDashboardStore`; use a user id, tenant id, store id, or any key you control. A null/empty key renders an empty container and never touches the store.
- **`EditMode`** — start in edit mode (add/remove/drag/resize widgets, rename, import/export). Defaults to `false`.
- **`ReadOnly`** — when `true`, hides the "Edit" affordance entirely so the dashboard cannot be edited.
- **`ShowDebugInfo`** — opt-in per-widget placement debug label in edit mode (off by default).
- **`GridOptions`** — a `DashboardGridOptions` to tune the grid: `Columns` (12), `CellHeight` (146px), `Margin` (8px), `Float` (true), `MobileBreakpointWidth` (992px), `MobileColumns` (1). Defaults match the kit's built-in behaviour.
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
        ConfigComponentType = typeof(MyConfigEditor)
    };
}
```

`TypedConfig` is the deserialized `TConfig` for the current placement. Set
`RequiresConfiguration = true` to force the config panel open before a newly
added widget is saved.

## Configure a widget

A widget's settings UI is a real Blazor component — full control over inputs,
layout, and validation. Point `WidgetDescriptor.ConfigComponentType` at a
component deriving `WidgetConfigComponent<TConfig>`; the dashboard renders it in
the config panel and owns Save/Cancel. Bind to the typed `Model` (an isolated
working copy) and call `NotifyChangedAsync()` after a change:

```razor
@inherits BlazorDashboardKit.Components.WidgetConfigComponent<MyWidget.MyConfig>

<input class="form-control" value="@Model.Title" @onchange="OnTitle" />

@code {
    async Task OnTitle(ChangeEventArgs e)
    {
        Model.Title = e.Value?.ToString() ?? "";
        await NotifyChangedAsync();
    }
}
```

Leave `ConfigComponentType` null for a widget with no configuration (the panel
then shows a "no configuration" message).

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

`DashboardHost` works in Blazor Server, WebAssembly, and static SSR. It is render-mode-safe: during static SSR and prerender it emits markup only and never invokes JavaScript; the grid is initialized lazily once the component reaches an interactive render. Until GridStack is live (static SSR, prerender, and the brief pre-interactive window) the kit applies a CSS fallback so widgets render in a readable stacked flow instead of collapsing — so a static-SSR dashboard degrades gracefully rather than breaking. For static SSR, pass `ReadOnly="true"` so non-functional edit affordances are not emitted. The in-memory store is per-process, so if you render the same dashboard under different interactivity locations (e.g. a server-prerendered page that becomes WebAssembly-interactive) back it with a shared store (`UseJsonFileStore` or a custom `IDashboardStore`) registered identically on every side.

A standalone Blazor WebAssembly sample (the live demo above) lives in `samples/StandaloneWasm`; the Blazor Web App sample (Server + WASM + SSR pages) is in `samples/SampleApp`.

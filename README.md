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

`AddDashboardWidget<TComponent>(descriptor)` registers a widget type with its descriptor; call it once per widget. **Where** to register it depends on the render mode — see [Render modes & hosting](#render-modes--hosting) (the short version: register in every DI container the render mode instantiates).

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
kit adopts a BTCPay theme automatically. It does **not** auto-switch on OS
`prefers-color-scheme` — a reusable kit must match its host, not override it
(a light host page would otherwise get a dark widget card). **Dark mode is
opt-in:** set the tokens (or inherit `--btcpay-*`). Override any token on an
ancestor (zero-specificity `:where()` selectors mean your values always win),
e.g.

```css
:root {
    --bdk-surface: #0b1020;
    --bdk-text: #e8eaf0;
    --bdk-primary: #6c8cff;
    --bdk-radius: 0.75rem;
}
```

Want the kit to follow the OS theme? Opt in from your app (so it only happens
where your page is also dark):

```css
@media (prefers-color-scheme: dark) {
    :root { --bdk-surface: #1e1e2f; --bdk-text: #e6e6ef; --bdk-border: rgba(255,255,255,.12); }
}
```

## Locking widgets

In edit mode each widget has a lock toggle (🔒/🔓) in its header. A locked
widget cannot be dragged or resized and other widgets won't displace it
(`WidgetPlacement.Locked`, persisted with the layout). Useful for pinning a
header or KPI strip while the rest of the dashboard stays rearrangeable.

## Theming tokens

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
- **`OnDashboardChanged`** — `EventCallback` raised after any change is persisted (add/remove/move/resize/config/rename/import), for external sync, analytics, or autosave hooks.
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

## Overriding the built-in pieces

Every UI piece the kit renders is replaceable on `DashboardHost`, two ways per
piece, with the same precedence everywhere:

**`…Template` (RenderFragment) > `…Component` (Type) > kit default.**

Each override receives a typed context (data + action `EventCallback`s). A
`…Template` is a `RenderFragment<TContext>`; a `…Component` is any component
declaring `[Parameter] public TContext Context { get; set; }`.

| Piece | Params | Context |
|---|---|---|
| Widget edit header | `WidgetHeaderTemplate` / `WidgetHeaderComponent` | `WidgetHeaderContext` — Placement, Descriptor, Configure/ToggleLock/Duplicate/Remove |
| Empty dashboard | `EmptyTemplate` / `EmptyComponent` | `EmptyDashboardContext` — EditMode |
| Widget unavailable | `WidgetUnavailableTemplate` / `WidgetUnavailableComponent` | `WidgetUnavailableContext` — Placement, EditMode, Remove |
| Widget error | `WidgetErrorTemplate` / `WidgetErrorComponent` | `WidgetErrorContext` — Exception, Placement, Descriptor, Recover |
| Add-Widget picker | `WidgetPickerTemplate` / `WidgetPickerComponent` | `WidgetPickerContext` — access-filtered Available, Add |
| Config-panel shell | `ConfigPanelTemplate` / `ConfigPanelComponent` | `WidgetConfigShellContext` — Body, HasConfig, Save, Cancel |

The kit keeps owning behavior the override shouldn't reimplement: the picker's
list is still access-filtered; the config shell still gets a deep-cloned working
copy with the kit's Save/Cancel semantics — your shell just supplies chrome
around `Context.Body` and wires its controls to `Save`/`Cancel`.

```razor
@* Brand the empty state (RenderFragment) and swap the header for your own
   component — anything you don't override keeps the kit default. *@
<DashboardHost OwnerKey="@userId" EditMode="true"
               WidgetHeaderComponent="typeof(MyWidgetToolbar)">
    <EmptyTemplate Context="ctx">
        <div class="my-empty">
            @(ctx.EditMode ? "Add a widget to begin" : "Nothing here yet")
        </div>
    </EmptyTemplate>
</DashboardHost>
```

```razor
@* MyWidgetToolbar.razor — a component override implements the seam contract *@
@code {
    [Parameter] public WidgetHeaderContext Context { get; set; } = default!;
}
<div class="my-toolbar">
    <span>@Context.Descriptor.Name</span>
    <button @onclick="Context.Configure">⚙</button>
    <button @onclick="Context.Remove">✕</button>
</div>
```

Two pieces are intentionally **not** override seams (the alternative is simpler
and already there): the **widget card wrapper** — restyle it with the per-widget
`WidgetDescriptor.CssClass` and the `--bdk-*` theming tokens rather than
replacing the element that hosts the header/body/error; and the **debug label**
— it is an opt-in dev affordance, leave `ShowDebugInfo` at its default `false`
to omit it.

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

## Render modes & hosting

`DashboardHost` is render-mode-safe. It touches no JavaScript until it reaches an interactive render (`OnAfterRenderAsync`), so static SSR and the prerender pass emit markup only; until GridStack is live (static SSR, prerender, and the brief pre-interactive window) a CSS fallback lays widgets out in a readable stacked flow instead of collapsing to 0×0. It works in every Blazor hosting model — the only thing that changes is *where you register it*.

**The one rule:** call `AddBlazorDashboard()` (and your `AddDashboardWidget<…>`) in **every DI container the render mode instantiates**, with the **same** widget set.

| Hosting model | Register `AddBlazorDashboard()` in | Widgets & WASM/Auto pages must live in | Store |
|---|---|---|---|
| Static SSR (no `@rendermode`) | server `Program.cs` | server project | any; pass `ReadOnly="true"` |
| Interactive Server | server `Program.cs` | server project | any |
| Interactive WebAssembly (Web App) | **server *and* `.Client`** `Program.cs` | the `.Client` project | shared (see below) |
| Interactive Auto (Web App) | **server *and* `.Client`** `Program.cs` | the `.Client` project | shared (see below) |
| Standalone Blazor WebAssembly | WASM `Program.cs` | the WASM project | shared if multi-device, else in-memory |
| Standalone Blazor Server | server `Program.cs` | server project | any |

### Static SSR

No `@rendermode`. The page renders once on the server with the CSS fallback and no JS. Pass `ReadOnly="true"` so non-functional edit affordances aren't emitted:

```razor
@page "/dashboard"
<DashboardHost OwnerKey="@userId" ReadOnly="true" />
```

### Interactive Server

`@rendermode InteractiveServer`. Full drag/resize/config over the SignalR circuit. Register on the server only. Prerendering (on by default) shows the server-rendered fallback first, then the grid initializes when the circuit connects — automatic, nothing to do.

### Interactive WebAssembly / Interactive Auto (Blazor Web App)

In a Blazor Web App the component runs in **two runtimes**: the server prerender pass *and* the browser (WebAssembly). Each has its own DI container, so `AddBlazorDashboard()` + your widgets must be registered in **both** `Program.cs` files (server and `.Client`). A page marked `@rendermode InteractiveWebAssembly`/`InteractiveAuto`, and every widget component it renders, must be in the **`.Client`** project (an assembly the browser downloads) — see `samples/SampleApp` (`Wasm.razor` lives in `.Client`; `Ssr.razor`/`Home.razor` in the server).

Because prerender (server process) and interactive WASM (browser) are different runtimes, the store must be reachable from **both**. The built-in `UseJsonFileStore` is **server-side only** — under WebAssembly `System.IO` is a throwaway in-browser virtual filesystem (not persisted, not shared with the server), so it is **not** valid on the `.Client`. The shared backing must be something the browser can reach too — typically your API:

```csharp
// Server Program.cs — real persistence lives here
builder.Services.AddBlazorDashboard(o => o.UseJsonFileStore("…"))   // or your DB
    .AddDashboardWidget<MyWidget>(MyWidget.Descriptor);

// SampleApp.Client/Program.cs — same widgets, an HTTP-backed store
builder.Services.AddBlazorDashboard()                 // default in-memory replaced ↓
    .AddDashboardWidget<MyWidget>(MyWidget.Descriptor);
builder.Services.AddScoped<IDashboardStore, ApiDashboardStore>();  // calls your minimal API
```

Register your `IDashboardStore` **before** `AddBlazorDashboard()` (it uses `TryAdd`, so yours wins). The store is your integration seam — the kit never assumes a filesystem, a DB, or an API.

> **Store ↔ render-mode validity:** `UseJsonFileStore` → static SSR ✅, Interactive Server ✅, WebAssembly/Auto ❌ (browser FS). In-memory (default) → fine for a single runtime; never for prerender→WASM. For WASM/Auto use an API/HTTP- or browser-storage-backed `IDashboardStore`.

### Standalone Blazor WebAssembly / Server

Single runtime, single `Program.cs` — register there. The standalone WASM demo (`samples/StandaloneWasm`, the live demo above) uses the default in-memory store; swap in a real `IDashboardStore` to persist.

Samples: `samples/SampleApp` is a Blazor Web App with Server (`/`), WASM (`/wasm`), and static SSR (`/ssr`) pages; `samples/StandaloneWasm` is a pure static WebAssembly app.

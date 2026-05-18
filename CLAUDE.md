# BlazorDashboardKit — project guide

Host-agnostic Blazor RCL: a draggable/resizable GridStack dashboard of pluggable widgets. Consumer supplies persistence (`IDashboardStore`) and access policy (`IWidgetAccessControl`). Library = `src/BlazorDashboardKit`; demo = `samples/SampleApp` (Server `/`, WASM `/wasm`, static SSR `/ssr`); tests = `tests/BlazorDashboardKit.Tests` (bUnit + xUnit).

## Build / run / test

- Test: `dotnet test tests/BlazorDashboardKit.Tests/BlazorDashboardKit.Tests.csproj`
- Run sample: `dotnet run --launch-profile http --no-build` in `samples/SampleApp/SampleApp` → http://localhost:5287
- **Editing RCL static assets** (`src/BlazorDashboardKit/wwwroot/*.css|*.js`): plain incremental build serves STALE assets. Use `dotnet build samples/SampleApp/SampleApp/SampleApp.csproj -c Debug -t:Rebuild`, then `curl` the served `_content/...` file to confirm; browsers also cache it (cache-bust when verifying).
- bUnit constraint: never `JSInterop.SetupModule` (documented VSTest-host hang). To assert JS interop, inject a fake `IJSRuntime` into `DashboardJsInterop`.

## E2E smoke test checklist

Run after any change to the dashboard host, widgets, interop, or CSS. Exercise on the **Server** page at minimum; spot-check WASM and SSR for render-mode parity. **Keep this list current: add a case when a feature is added; revise a case when behavior changes.**

- [ ] All pages load without console errors: SampleApp `/` (Server), `/wasm`, `/ssr`; StandaloneWasm `/` + `/usage`.
- [ ] No kit asset tags in the host page, yet `GridStack` is defined and `dashboard.css`/`gridstack.min.css` are present (JS initializer auto-injected them).
- [ ] **Add Widget** dropdown opens with NO Bootstrap JS loaded; widgets grouped by category; closes on select.
- [ ] Adding a widget renders it with real size/position (GridStack instance present; `bdk-grid-static` cleared; item rect non-zero, a grid cell not full-width).
- [ ] **Actions** dropdown opens; Export downloads JSON; Import loads a JSON file; menu closes after each.
- [ ] Drag a widget (edit mode) and resize it; positions/sizes persist after Done + reload.
- [ ] Remove (✕) deletes the widget and the grid re-lays-out the remaining widgets.
- [ ] A widget's own controls (e.g. Notes "Edit") are clickable — NOT overlapped/hijacked by the edit header's ✕.
- [ ] Configure (gear) opens the panel rendering the widget's **config component**; editing + Save persists and reflects in the widget; original config untouched on Cancel. Widget with null `ConfigComponentType` shows "no configuration" + no Save.
- [ ] A widget with `RequiresConfiguration` opens its config before the first save.
- [ ] Debug label hidden by default; appears only with `ShowDebugInfo="true"`.
- [ ] Edit ⇄ Done toggles edit affordances; Done persists; reload shows the persisted dashboard.
- [ ] Rename the dashboard title; it persists across reload.
- [ ] `/ssr` renders read-only (no edit affordances) with a usable CSS-fallback layout — no 0×0 collapse, no crash; `AlwaysInteractive` widgets render read-only there too.
- [ ] StandaloneWasm: an interactive widget (Counter) increments and the value survives reload (config-persisted).
- [ ] Resizing a widget honors the descriptor's Min/Max column/row size (gs-min/max-* emitted).
- [ ] `GridOptions` (e.g. Columns=6) changes the grid layout; defaults match built-in behaviour when omitted.
- [ ] Under OS dark mode the kit auto-uses the dark palette; an explicit `--bdk-*` override still wins.
- [ ] Picker is filtered by `IWidgetAccessControl` when one is registered.
- [ ] Publish: a `<Version>` bump on `main` triggers `publish.yml` → NuGet + GitHub Packages + `v<version>` tag (no-op if the tag exists). `pages.yml` deploys the StandaloneWasm demo on push to `main`.

## Conventions

- Bugs/behavior changes: failing test first (TDD), root-cause before fixing (no symptom patches).
- No hardcoded BTCPay theme tokens in the library — use CSS custom properties with sane fallbacks so it is themable (and reusable in BTCPay later); sample supplies its own theme.

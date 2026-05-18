# Changelog

All notable changes to BlazorDashboardKit are documented here. The format is
based on [Keep a Changelog](https://keepachangelog.com/); this project aims to
follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- **Multiple dashboards** per owner: switch via a header selector, add and
  delete dashboards (the last one is protected). The data model already
  supported a collection; this exposes it in the UI and persists the active
  selection.
- Per-widget **lock** (`WidgetPlacement.Locked`): pin a widget so it can't be
  moved or resized; toggle from its edit-mode header. Persisted with the layout.
- **Duplicate widget** action in the edit-mode header: clones a widget with a
  deep-copied (independent) config.
- `DashboardHost.OnDashboardChanged` event raised after every persisted change
  (external sync / analytics / autosave hooks).
- Dropdowns (widget picker, Actions) now close on click-outside via a
  pure-Blazor transparent backdrop — still no Bootstrap JS.
- `aria-label`s on the icon-only widget control buttons.
- Destructive actions (remove widget, delete dashboard) now require a two-step
  inline confirm — guards against the accidental-loss class of mistake.
- **Component-based widget configuration**: a widget points
  `WidgetDescriptor.ConfigComponentType` at a component deriving
  `WidgetConfigComponent<TConfig>`, replacing the schema dictionary.
- Configurable grid via `DashboardHost.GridOptions` (`DashboardGridOptions`):
  columns, cell height, margin, float, mobile breakpoint/columns.
- Per-widget min/max size from the descriptor is now enforced by GridStack
  (`gs-min/max-w/h`).
- Themeable CSS via `--bdk-*` custom properties (standalone defaults that also
  map to BTCPay variables); automatic dark mode under `prefers-color-scheme`.
- CSS fallback layout so the grid degrades gracefully under static SSR /
  prerender / the pre-interactive window instead of collapsing.
- Blazor JS initializer auto-injects the kit's CSS + GridStack — no manual host
  asset tags required.
- Opt-in per-widget debug label (`DashboardHost.ShowDebugInfo`).
- Standalone Blazor WebAssembly sample (live demo + usage docs), deployed to
  GitHub Pages.
- `publish.yml` (tag-based release to NuGet + GitHub Packages) and `pages.yml`.

### Fixed
- `JsonFileDashboardStore` no longer crashes the dashboard on a corrupt/partial
  persisted file: it quarantines the bad file (`.corrupt-<ts>`) and recovers.
- Widget picker / Actions menus no longer require Bootstrap's JS (Blazor-driven).
- Grid is re-initialized after add/remove/import (was stranded after the first
  layout change).
- `RemoveWidget` rebuilds the grid instead of leaving GridStack's model stale.
- The edit-mode header no longer overlaps and steals clicks from a widget's own
  controls.
- Host `ReadOnly` / non-interactive render now forces widgets read-only even
  when `AlwaysInteractive`.

### Changed
- **Breaking:** `WidgetDescriptor.ConfigSchema` and the `ConfigFieldSchema` /
  `ConfigFieldType` / `SelectOption` types were removed in favour of
  `ConfigComponentType` + `WidgetConfigComponent<TConfig>`.

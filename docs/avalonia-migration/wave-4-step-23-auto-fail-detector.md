# Wave 4, step 23: Auto-fail Detector

Status: implemented, 2026-08-06.

## Scope delivered

Auto-fail Detector is now a complete Avalonia tool slice. Manual execution uses
the first path in the shared beatmap workspace, while QuickRun and the
`AlwaysQuickRun` preference inspect the beatmap currently open in osu!. The
command is registered at host startup with the legacy display name and the
`Always` Smart QuickRun target, without constructing or reflecting over views.

The view preserves the legacy defaults for confirmed objects, potential
objects, disruptors, AR/OD overrides, physics-update leniency, fix guidance,
and automatic fix placement. Results retain the legacy summary wording and are
projected into the shared Timeline using removed/red, added/green, and
accent/purple semantic markers. Click navigation is routed through the
platform launcher rather than starting a process from the control.

## Shared algorithm and safety boundaries

The detection and fix-planning algorithm now lives in
`Mapping_Tools.Core.Tools.AutoFail.AutoFailDetectorEngine`. It has no WPF,
Avalonia, process, filesystem, or dialog dependencies. The legacy WPF detector
is a small compatibility adapter over that same engine, so both frontends use
one implementation.

`AutoFailService` owns live-aware beatmap loading and difficulty override
resolution. Analysis is read-only and creates no backup. An accepted automatic
fix is persisted exclusively through `IBeatmapEditingGateway.SaveAsync`, which
requires a successful safety copy before overwriting the beatmap. The shared
tool-execution coordinator owns cancellation, progress, notifications, and the
optional osu! editor reload.

## Automated and build coverage

The accepted positive fixture reproduces 20 confirmed and 63 potential
unloading objects. The accepted negative fixture reports no auto-fail. Service
tests cover live-aware opening and backup-gateway persistence; view-model tests
cover manual workspace selection, exact summary text, marker filtering, and
startup QuickRun dispatch against the current osu! map.

All 59 Core tests, 184 platform tests, and 3 architecture tests pass. The full
Release solution builds, including Avalonia, WPF, and both renderer tools.

Per the user's explicit instruction, no PNG rendering, screenshot comparison,
or other visual validation was performed. AXAML and compiled bindings were
validated by the build.

## Documentation consulted

- <https://docs.avaloniaui.net/api/avalonia/controls/numericupdown>
- <https://docs.avaloniaui.net/controls/input/selectors/checkbox>
- <https://docs.avaloniaui.net/docs/data-binding/introduction-to-data-binding>
- <https://docs.avaloniaui.net/docs/custom-controls/custom-control-class>
- <https://docs.avaloniaui.net/docs/events/input-events>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>
- <https://www.nuget.org/packages/Avalonia/12.1.0>

The feature uses the existing compiled-binding view convention and the shared
Step 22 timeline control rather than introducing feature-local drawing code.

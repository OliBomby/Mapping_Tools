# Wave 4, step 24: Map Cleaner

Status: implemented, 2026-08-06.

## Scope delivered

Map Cleaner is now available in the Avalonia Tools section with its legacy
cleaning, resnapping, bookmark, hitsound, muting, sample-use, and map-information
options. Manual execution processes the shared workspace selection in order;
QuickRun and `AlwaysQuickRun` process the beatmap currently open in osu!. Its
startup command retains the `Map Cleaner` name and `Always` Smart QuickRun
target.

The feature retains typed project save/open/new and autosave behavior. The
trusted legacy serializer maps `MapCleanerVm`, `MapCleanerArgs`, and the legacy
beat-divisor types to their migrated models and emits the legacy aliases again
when saving.

For a single map, added, changed, and removed greenlines appear in the shared
Timeline with the Step 22 semantic colors. Click navigation is routed
through the injected platform launcher. Multi-map runs intentionally omit one
ambiguous combined timeline while retaining aggregate result counts.

## Shared transformation and safety boundaries

The deterministic timing/object transformation now lives in
`Mapping_Tools.Core.Tools.MapCleaner.MapCleanerEngine`. It owns resnapping,
timing-effect reconstruction, hitsound-state rewriting, 2B double-tap ordering,
progress, cancellation, and framework-neutral timing-point differences. It has
no WPF, Avalonia, process, dialog, or filesystem dependency. The legacy WPF
tool delegates to this same engine.

`MapCleanerService` opens every map with live-state preference and persists each
result exclusively through `IBeatmapEditingGateway.SaveAsync`, which requires a
successful safety copy before overwrite. Sample analysis and folder mutation
live in Infrastructure. Unused samples in the Avalonia workflow are moved into
a timestamped `.mapping-tools-unused-samples` recovery folder instead of being
irreversibly deleted; mapset-wide beatmaps, storyboards, spinner samples, and
skinnable samples are considered before a move.

## Automated and build coverage

The accepted `standard-feature-rich.osu` fixture produces the exact accepted
output document: 16 greenlines removed, 20 objects resnapped, 815 timing
points, 924 hit objects, and 20 bookmarks. Service tests cover live-aware input
and backup-gated persistence. View-model tests cover workspace routing, legacy
summary wording, timeline markers, progress, and current-map QuickRun.
Legacy project alias round-trip and dependency-injection validation are also
covered.

All 60 Core tests, 188 platform tests, and 3 architecture tests pass. The full
Release solution builds including Avalonia, WPF, and both renderer tools.

Per the user's explicit instruction, no PNG rendering, screenshot comparison,
or other visual validation was performed. AXAML and compiled bindings were
validated by compilation.

## Documentation consulted

- <https://docs.avaloniaui.net/controls/input/selectors/checkbox>
- <https://docs.avaloniaui.net/docs/data-binding/introduction-to-data-binding>
- <https://docs.avaloniaui.net/docs/custom-controls/custom-control-class>
- <https://docs.avaloniaui.net/docs/events/input-events>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>
- <https://www.nuget.org/packages/Avalonia/12.1.0>

The view follows the existing compiled-binding convention and reuses the shared
Timeline control rather than adding feature-local rendering code.

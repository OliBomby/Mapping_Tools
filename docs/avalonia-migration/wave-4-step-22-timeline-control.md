# Wave 4, step 22: shared Timeline control

Status: implemented, 2026-08-06.

## Scope delivered

The Avalonia Desktop now has a reusable `TimelineControl` for timestamped tool
findings. It replaces the legacy WPF control's dependence on `MainWindow`,
imperatively created child controls, fixed construction-time dimensions, WPF
brushes, and direct `Process.Start` navigation.

Consumers supply an immutable snapshot of framework-neutral `TimelineMarker`
records, a start/end viewport, and an optional navigation command. Marker kinds
preserve the legacy neutral, added/green, changed/yellow, removed/red, and
accent/purple semantics without placing colors or brushes in Application.
Clicking the nearest marker invokes the command with its millisecond timestamp.
Hover uses the same deterministic hit testing and displays a normalized
timestamp tooltip.

`TimelineScale` owns the non-visual behavior: the legacy 20-millisecond minimum
span, eleven inclusive ticks, clamped timestamp projection, stable nearest-hit
selection for dense/overlapping markers, and formatting that does not wrap
minutes after one hour. Milliseconds are consistently formatted with three
digits.

## Avalonia implementation

The control derives from `Control` and draws ticks, the center line, and marker
strokes in one `DrawingContext.Render` pass. Styled properties participate in
binding and trigger `AffectsRender`; resize automatically changes projection,
so no global-window-width recalculation remains. Drawing is clipped to the
control bounds, and invalid transient viewport values fall back safely.

All literal marker colors live in the central `MappingToolsColors.axaml`
resource dictionary. An application style maps those dynamic theme resources
to the control's brush properties. The Desktop marker/scale types contain no
Avalonia, WPF, WinForms, brush, process, or command dependency, but remain
presentation-owned because they model viewport, formatting, and hit testing.

The timeline types were initially kept in `Mapping_Tools.Application` because
they were framework-neutral. They are now owned by
`Mapping_Tools.Desktop/Controls/Timeline`; the Application layer supplies the semantic
finding data that Desktop maps into markers.

The legacy control did not implement scrolling or zoom, so this parity slice
does not invent those behaviors. Auto-fail Detector and Map Cleaner remain the
first consumers and will bind marker snapshots when migrated in steps 23 and
24. URI navigation will be supplied by their injected platform-launch command.

## Automated and build coverage

All 180 platform tests pass. Timeline tests cover empty/minimum ranges, inclusive
ticks, boundary and out-of-range projection, dense overlapping-marker hit
resolution, hour-plus timestamp formatting, and arranged-control hit testing.
All 3 architecture tests pass, and the full Release solution builds including
both frontends and renderer tools. Boundary and centralized-color searches are
clean.

Per the user's explicit instruction, no PNG rendering, image comparison, or
other visual validation was performed. The custom control and application
style are validated by compilation.

## Documentation consulted

- <https://docs.avaloniaui.net/docs/custom-controls/custom-control-class>
- <https://docs.avaloniaui.net/docs/custom-controls/drawing-custom-controls>
- <https://docs.avaloniaui.net/docs/graphics-animation/custom-rendering>
- <https://docs.avaloniaui.net/docs/custom-controls/defining-properties>
- <https://docs.avaloniaui.net/docs/events/input-events>
- <https://docs.avaloniaui.net/docs/input-interaction/pointer>
- <https://docs.avaloniaui.net/api/avalonia/input/pointerpressedeventargs>
- <https://docs.avaloniaui.net/api/avalonia/media/drawingcontext>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>
- <https://www.nuget.org/packages/Avalonia/12.1.0>

The local Avalonia 12.1.0 reference assemblies were checked for `AffectsRender`,
`DrawingContext` clipping/opacity operations, pointer click counts, relative
positions, cursor behavior, and styled-property registration.

# Wave 11 — Step 47 Avalonia/WPF parity audit

Status: implemented 2026-08-18.

This audit covers the migrated feature work assigned to graph steps 37–46. The
legacy WPF view, view model, converters, custom controls, event handlers,
project serializers, platform adapters, updater gateway, and registration
were treated as the normative specification. Graph steps 48 (default
executable switch) and 49 (legacy removal) are explicitly out of scope.

## Audit coverage

| Step | Feature | Audited behavior and disposition |
| --- | --- | --- |
| 37 | Pattern Gallery | Restored the WPF selectable-list contract with Avalonia `ListBox` containers, moved collection actions into the shell project menu, preserved the WPF help copy/tooltips, and made thumbnail loading cancel safely across project/file replacement. The Desktop `PatternThumbnailControl` is the presentation-side rendering substitution for the WPF bitmap thumbnail converter. |
| 38 | Graph and value editor | Compared graph state cloning, bounds, interpolation, marker generation/snapping, context-menu commands, typed-value dialog, pointer gestures, keyboard deletion/Escape, wheel modifiers, and cancellation/clone semantics. Wheel zoom now ignores positions outside the graph bounds, matching WPF. Cursor warping remains the documented platform substitution. |
| 39 | Sliderator | Compared import modes, lost-focus converters and validation ranges, graph bounds/markers, preview timing, navigation and Shift quick placement, empty-source branches, ordinary/QuickRun execution, persistence, and registration. No remaining parity defect was substantiated. |
| 40 | Tumour Generator 2 | Compared layer commands/order, graph validation and serialization, import/empty states, preview cancellation/latest-request ownership, activation/disposal, ordinary/QuickRun completion/error branches, and registration. Preview work is now inactive until shell activation, and the migrated completion message matches WPF spelling. |
| 41 | Audio | Compared decode/generation/playback ownership, cancellation, preview session disposal, format compatibility, and architecture boundaries. |
| 42 | Hitsound Studio | Compared layer selection/editing, add/remove/reorder keyboard behavior, import/reload/preview/validation/export dialogs, empty/error/completion branches, legacy project-root serialization, audio-session disposal, and project registration. No remaining parity defect was substantiated. |
| 43 | Geometry Dashboard core/project models | Compared generator reflection order, settings and locked-object ownership, legacy JSON names/colours/hotkeys, type-keyed settings, collection invariants, and Core/Application boundaries. Existing compatibility tests cover the serializer and locked-object formats. |
| 44 | Windows adapters | Compared process/title discovery, editor-reader snapshots, global hotkeys, physical-pixel/DPI conversion, click-through overlay lifecycle, non-Windows no-op behavior, and exception guards. No remaining parity defect was substantiated. |
| 45 | Geometry Dashboard UI | Compared generator grouping/filtering, specialized inner scrolling and wheel/mouse ownership, toggle/configure actions, empty/status branches, dialogs, hotkeys, progress, and registration. The inner generator scroller remains view-owned as in WPF; the `ToggleSwitch` and generator-group `ItemsControl` are approved Avalonia substitutions. The progress color is now owned by the application resource dictionary. |
| 46 | Updater | Compared release metadata/asset selection, skip policy, preparation progress/cancellation, install-now/install-after-close/skip branches, owner-modal windows, close lifecycle, Onova ownership, and project/build registration. `UpdateService` now waits for in-flight preparation and coordination before disposing its gateway. |

## Corrective changes

- Moved the Geometry Dashboard progress color from view markup into
  `MappingToolsColors.axaml`.
- Restored Pattern Gallery's `ListBox`/`ListBoxItem` selection semantics and
  shell-owned extra project commands; removed duplicate feature-local project
  actions and non-WPF selection controls.
- Added cancellation and stale-project guards around Pattern Gallery thumbnail
  refreshes when files, collections, or projects change.
- Matched GraphControl wheel-boundary behavior to WPF and removed its stale
  `IsVisible` name collision with Avalonia's base visual API.
- Kept Tumour Generator preview work inactive until `Activate` and corrected
  its migrated success message.
- Restored Hitsound Studio's two extra project commands to the shell-owned
  project menu, removed the duplicate feature-local menu, and kept UI-bound
  continuations on the UI context while retaining a non-blocking disposal
  continuation for the synchronous host bridge.
- Gave each `ValueOrGraphControl` instance its own default graph state instead
  of sharing a mutable Avalonia property default between controls.
- Made updater disposal await the active package preparation and check gate,
  while retaining the synchronous `IDisposable` contract used by the host.
- Added focused parity, lifecycle, registration, and disposal tests.

## Explicitly accepted substitutions

The audit does not treat framework-only replacements already recorded by the
wave notes as parity defects: shared Avalonia tool controls, native owner-modal
dialogs, `ItemsControl` grouping where Avalonia has no WPF
`CollectionViewSource` equivalent, the reusable object renderers, the
Windows adapter boundary, and Onova's updater gateway. These substitutions
preserve the WPF behavior and are kept at their documented ownership boundary.

## Verification

- `git diff --check`: passed.
- Full relevant test matrix: 632 passed across Core, Application, Desktop,
  Infrastructure, Architecture, and the legacy WPF test project.
- Relevant focused tests: updater disposal, Pattern Gallery parity, Geometry
  resource ownership, GraphControl interaction, and Tumour lifecycle all pass.
- Avalonia Desktop build: 0 errors (24 existing nullable/platform warnings).
- Legacy WPF frontend build: 0 errors (2 existing platform/package warnings).
- Core, Application, Infrastructure, and Architecture builds/tests completed
  through the full solution verification.

No commit was created. Steps 48 and 49 were not implemented, and no legacy
project was removed.

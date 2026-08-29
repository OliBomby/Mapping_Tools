# Wave 10, step 45: Geometry Dashboard UI

Step 45 ports the Geometry Dashboard presentation layer to Avalonia while keeping the step-43 Core graph/project models and the neutral application Geometry Dashboard contracts as the boundaries.

## Preserved behavior

- Generator discovery, grouped filtering, active/sequential/relevancy editing, typed generator settings, and selection-predicate editing remain in the dashboard UI.
- Preferences, save slots, project autosave integration, generator actions, locked-object import/export, modifier-aware bulk toggles, and modeless save-slot editing remain available.
- The application `GeometryDashboardService` consumes only the semantic runtime, osu-space input, and osu-space overlay contracts. It owns the calculation loop, timer, state, layer collection, and overlay scene construction, preserving graceful no-osu/editor/platform states, editor-reader failure reporting, root-object selection, generator regeneration, snapping, selection, locking, inheritable state, and disposal.
- Desktop owns the `GeometryDashboardLifecycleCoordinator` and hosted-service adapter. The coordinator applies `KeepRunning`: the service follows view activation when it is disabled, and follows application startup/shutdown when it is enabled. The view model only projects service state and forwards UI actions.
- Overlay geometry is built in the application layer from Core shapes and preferences. Infrastructure owns configuration reads, immutable coordinate transforms, live window/DPI refresh, and painting through the click-through native host.

## Unavoidable Avalonia substitutions

- Avalonia 12.1 does not provide WPF `CollectionViewSource` grouping. The grouped generator list therefore uses an `ItemsControl` of expanded group rows with nested generator rows. The group headings, ordering, filtering, and row controls are retained; the dashboard's specialized inner generator scroller remains view-owned like the WPF view.
- WPF custom chrome and `Window.ShowDialog` were replaced with the existing Avalonia borderless-window/drag pattern and owner-modal windows.
- WPF `HotkeyEditorControl` is represented by the shared `HotkeyEditor`, which stores the neutral Core key/modifier representation and preserves delete/backspace/escape clearing and modifier-only suppression for Geometry Dashboard settings and save slots.
- The application overlay contract accepts `GeometryDashboardOverlayScene` primitives in osu! coordinates and `GeometryDashboardOverlayOptions`; Infrastructure performs all native placement and conversion while preserving click-through, nonactivating behavior. No native or screen-space types are exposed to the application.
- The WPF `DispatcherTimer`/`FileSystemWatcher` pair is represented by the application service worker and Infrastructure configuration refresh. Desktop controls service start/stop policy, while the service itself starts and stops only when explicitly told to do so.
- `KeepRunning` is a Desktop-owned property of `GeometryDashboardProject`, not a Core preference. Legacy project files that stored it under `CurrentPreferences` are read compatibly and migrated to the project-level property on load.

Later-wave updater, parity/cutover, and legacy-removal work is intentionally outside this step.

# Wave 10, step 45: Geometry Dashboard UI

Step 45 ports the Geometry Dashboard presentation layer to Avalonia while keeping the step-43 Core graph/project models and step-44 Application platform ports as the boundaries.

## Preserved behavior

- Generator discovery, grouped filtering, active/sequential/relevancy editing, typed generator settings, and selection-predicate editing remain in the dashboard UI.
- Preferences, save slots, project autosave integration, generator actions, locked-object import/export, modifier-aware bulk toggles, and modeless save-slot editing remain available.
- The activation loop consumes only the step-44 runtime/input/screen/window/overlay contracts. It preserves graceful no-osu/editor/platform states, editor-reader failure reporting, coordinate conversion, root-object selection, generator regeneration, snapping, selection, locking, inheritable state, hotkeys, and disposal.
- Overlay geometry is built in Desktop from Core shapes and preferences. Infrastructure only paints the neutral frame through the existing click-through native host.

## Unavoidable Avalonia substitutions

- Avalonia 12.1 does not provide WPF `CollectionViewSource` grouping. The grouped generator list therefore uses an `ItemsControl` of expanded group rows with nested generator rows. The group headings, ordering, filtering, and row controls are retained; the dashboard's specialized inner generator scroller remains view-owned like the WPF view.
- WPF custom chrome and `Window.ShowDialog` were replaced with the existing Avalonia borderless-window/drag pattern and owner-modal windows.
- WPF `HotkeyEditorControl` was replaced by `GeometryHotkeyEditor`, which stores the same numeric Core key/modifier representation and preserves delete/backspace/escape clearing and modifier-only suppression.
- Step 44's native host originally drew only the debug border. Step 45 adds `SetFrame(GeometryDashboardOverlayFrame)` to the Application contract so Infrastructure can rasterize the Desktop-built neutral geometry while preserving click-through, nonactivating overlay behavior. No native types are exposed to Core or Application.
- The WPF `DispatcherTimer`/`FileSystemWatcher` pair is represented by the view-model lifetime loop and configuration re-read through `ITextFileStore`; the loop is cancelled/disposed with the feature and honors `KeepRunning`.

Later-wave updater, parity/cutover, and legacy-removal work is intentionally outside this step.

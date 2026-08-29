# Wave 10 step 44 — I2/I3 Windows adapters

Status: implemented. This note covers the Windows support boundary required by
Geometry Dashboard; the dashboard, preferences, save-slot, generator-settings,
and overlay visualization UI remain step 45.

## Normative legacy sources

The adapter contract was derived from the complete legacy integration path:

- `Mapping_Tools/Viewmodels/GeometryDashboardVm.cs`
- `Mapping_Tools/Views/GeometryDashboard/GeometryDashboardOverlay.cs`
- `Mapping_Tools/Views/GeometryDashboard/GeometryDashboardView.xaml.cs`
- `Mapping_Tools/Classes/ToolHelpers/EditorReaderStuff.cs`
- `Mapping_Tools/Classes/SystemTools/ListenerManager.cs`
- `Mapping_Tools/Classes/SystemTools/Hotkey.cs`
- `Mapping_Tools/Classes/SystemTools/ActionHotkey.cs`
- `Mapping_Tools/Classes/Tools/GeometryDashboard/CoordinateConverter.cs`
- `Mapping_Tools/Classes/Tools/GeometryDashboard/Serialization/GeometryDashboardSaveSlot.cs`
- all I2/I3 call sites in the Geometry Dashboard view model and overlay.

## Step-44 surface

Infrastructure defines the desktop-only ports and adapters for:

- exact osu! stable process discovery;
- validated editor-memory snapshots containing the current path, AR, CS,
  editor time, and selected hit objects;
- exact legacy hotkey state and left-button state;
- monitor enumeration, primary-monitor selection, physical bounds, working
  areas, and effective DPI;
- top-level window enumeration, process-main-window selection, foreground
  activation, physical bounds, and effective DPI; and
- target-bound overlay lifecycle, activation visibility, DPI conversion, debug
  border, invalidation, and deterministic disposal. The application-facing
  overlay contract accepts only osu!-space scenes and neutral options.

`WindowsGeometryDashboardRuntimeService` provides the application-facing read
sequence: discover osu!, select its current main window, read validated editor
memory, and return only semantic editor state plus activation. A window title
that does not end in `.osu` is rejected before memory access. Missing
process/window/editor state returns `null` in that order; cancellation and
reader-validation exceptions are not swallowed.

`WindowsEditorReaderAdapter` implements the new editor snapshot port in
addition to the existing I1 application ports. It shares the existing reader
lock, process identity helper, validation, conversion, and diagnostic log; it
does not introduce another Editor Reader or memory interop path.

## Preserved behavior

- Process discovery accepts only a process named `osu!` whose main module is
  exactly `osu!.exe` and whose product name is exactly `osu!`; inaccessible or
  exited processes are skipped.
- A process without a main window title ending in `.osu` is treated as having
  no active editor. A missing process/editor returns `null`; malformed memory
  data follows the existing validated-reader failure and diagnostic-log path.
- Screen and window rectangles are expressed in physical desktop pixels,
  including negative coordinates on a virtual desktop. The coordinate context
  selects the monitor containing the osu! window and falls back to the Windows
  primary monitor when that lookup is unavailable.
- DPI scales are effective-DPI / 96. When Windows cannot supply DPI, the
  adapter reports a `Vector2.One` fallback with `DpiSourceAvailable == false`.
  The overlay preserves the legacy no-source path; with a live source it
  divides each bound by its axis scale and adds the legacy `(0.1, 0.1)` offset.
- Hotkey state requires the key and every Alt, Control, Shift, and Windows
  modifier to match the persisted WPF modifier mask exactly. Left/right
  modifier variants are equivalent. Invalid persisted key values are treated
  as inactive by polling, while the existing global callback adapter retains
  its validation behavior.
- Cursor reads and writes use osu! editor coordinates at the application
  boundary. Infrastructure maps to and from absolute physical desktop pixels,
  rounding only before the final Windows call. The left mouse button remains
  exposed for the held-object path.
- The overlay is a non-activating, tool-window, click-through popup. It hides
  whenever the tracked osu! window is not foreground, follows the editor bounds
  while active, supports the green-yellow three-pixel debug border, and makes
  repeated disposal safe.
- Global callback hotkeys now guard all native hook calls on non-Windows
  platforms. Start/stop and binding updates remain safe no-ops there so the
  rest of the application can be tested headlessly.
- Process exit races, stale/reused main-window handles, malformed reader
  collections, invalid DPI, and failed native overlay placement leave the
  adapter unavailable or hidden instead of exposing stale state as active.

## Required platform substitutions

- The legacy `Process.NET.Windows.IWindow` wrapper is replaced by a shared
  `user32.dll` window adapter kept entirely in Infrastructure, retaining title,
  activation, bounds, and lifetime checks.
- WinForms `Screen`, `Cursor`, and `Control.MouseButtons` are replaced by
  shared `user32.dll`/`shcore.dll` adapters. Screen, window, process, and
  platform-coordinate DTOs remain entirely in Infrastructure.
- `Overlay.NET.Wpf.OverlayWindow` is replaced by a small native popup host.
  `WindowsGeometryDashboardOverlayService` owns positioning, activation
  visibility, click-through behavior, border state, coordinate conversion, and
  disposal. It renders the application-provided osu!-space scene.
- Win32 APIs require integer window coordinates. The adapter rounds the
  converted logical position and size at the final `SetWindowPos` call; all
  preceding geometry remains double-precision Core data.

## Scope boundary

The Geometry Dashboard runtime, input, coordinate, and overlay boundaries are
now migrated. Generator controls, project/preferences windows, and save-slot
commands remain Desktop presentation work.

## Verification

Focused tests cover unavailable-platform process, editor-memory, input, screen,
window, overlay, and global-hook paths, semantic runtime sequencing, malformed
reader data, neutral snapshot copying, legacy key translation, immutable
coordinate transforms, live window movement, configuration reloads, and
physical-coordinate/DPI contracts. Architecture tests assert that the
application Geometry Dashboard contracts contain no native process, window,
input, overlay, or interop types.

## Avalonia migration references consulted

No Avalonia control or view API was changed in step 44. The required version
references were nevertheless checked before reviewing the Desktop composition
boundary:

- https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- https://docs.avaloniaui.net/docs/migration/wpf
- https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.0

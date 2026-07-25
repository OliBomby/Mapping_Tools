# Wave 2, step 15: QuickRun semantics and global hotkeys

Status: implemented, 2026-07-25.

## Scope delivered

The Application layer now owns QuickRun discovery and routing without
reflecting over WPF controls or retaining view instances:

- `QuickRunCommand` carries a stable identifier, the legacy-compatible display
  name, supported zero/one/many selection targets, and an asynchronous
  callback;
- `IQuickRunCommandRegistry` owns deterministic registration order, rejects
  ambiguous identifiers and display names, tracks the command selected by the
  in-app shell, and supplies filtered smart-target choices;
- `IQuickRunService` resolves either the current command or the configured
  Smart QuickRun command and returns typed outcomes for unavailable editor
  state, stale configuration, missing current command, and captured failure;
- `IGlobalHotkeyService` keeps global keyboard registration behind a
  platform-neutral Application boundary.

Smart routing deliberately counts hit objects selected in osu!, matching the
legacy `ListenerManager`. It does not count beatmap paths selected in the
Mapping Tools workspace. Zero, one, and multiple selected objects use
`NoneQuickRunTool`, `SingleQuickRunTool`, and `MultipleQuickRunTool`
respectively. The exact `<Current Tool>` sentinel selects the registry's
current command. Disabling `SmartQuickRunEnabled` avoids Editor Reader
entirely and invokes the current command.

`AlwaysQuickRun` remains a policy for ordinary feature Run actions; it never
changed global shortcut routing in the legacy application. Its Application
XML documentation now states that distinction.

## Windows shortcut adapter

`WindowsGlobalHotkeyService` uses the existing
`NonInvasiveKeyboardHookLibrary.Core` binary already shipped with Mapping
Tools. It translates persisted WPF key-enum numbers into Win32 virtual-key
values without referencing WPF from Infrastructure. The conversion covers
letters, top-row and numpad digits, function keys, navigation keys, and the
editing keys supported by the legacy settings format. Alt, Control, Shift,
and Windows modifier bits retain their existing numeric representation.

The Generic Host starts one listener with bindings for QuickRun and QuickUndo
and unregisters and stops it during application shutdown. Disabled or null
hotkeys remain unregistered. Callback work is moved away from the hook thread
and receives host-shutdown cancellation.

QuickUndo now has an Application command service shared by global and future
in-app invocation. It locates osu!'s current map, applies the newest compatible
backup through the step 13 service, honors `AutoReload`, and publishes typed
success, warning, or failure outcomes.

## Legacy compatibility and deferred UI

The WPF `IQuickRun`, attributes, `ViewCollection`, and `ListenerManager`
remain operational for unmigrated features. Avalonia feature slices register
commands explicitly as they arrive; no feature view is constructed merely to
discover QuickRun support.

The temporary Avalonia shell has no migrated tool screens yet, so its registry
is initially empty and a shortcut produces the typed no-current-command
outcome. Wave 3 supplies navigation selection, smart-target preferences,
hotkey editing and live rebinding, and user-visible notification presentation.
BetterSave and its file-watcher override remain part of that shell workflow;
they are not required for the A7 QuickRun command boundary or the Wave 2 exit
condition.

The global adapter is Windows-only. Native shortcut capture and conflicts with
other processes require a real desktop smoke test; they cannot be asserted
reliably in the headless suite.

## Automated coverage

`Mapping_Tools.Platform.Tests` verifies:

- deterministic registry ordering, duplicate rejection, current-command
  removal, and per-target filtering;
- exact zero, one, and multiple selected-object routing;
- current-tool fallback and smart-routing bypass;
- unavailable editor, stale configured tool, live-read failure, command
  failure, notification severity, and cancellation behavior;
- legacy fixture conversion for QuickRun, BetterSave, and QuickUndo keys;
- Generic Host wiring from both global bindings to their Application commands
  and listener shutdown;
- QuickUndo current-map, empty-history, reload, success, and failure behavior;
- the complete headless Wave 2 acceptance workflow.

The focused platform suite passes 101 tests. This step adds no AXAML or visual
state, so no render baseline applies.

## Documentation consulted

The existing classic-desktop lifetime remains the point at which the Generic
Host starts and stops the global listener:

- <https://docs.avaloniaui.net/docs/fundamentals/application-lifetimes>

Windows keyboard and virtual-key behavior:

- <https://learn.microsoft.com/en-us/windows/win32/inputdev/about-keyboard-input>

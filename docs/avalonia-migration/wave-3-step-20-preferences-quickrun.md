# Wave 3, step 20: Preferences pass 2 and QuickRun UI

Status: implemented, 2026-08-06.

## Scope delivered

The Avalonia Preferences feature now exposes the remaining legacy cross-cutting
settings:

- automatic replacement of focused osu! saves with BetterSave;
- automatic editor reload after QuickRun and the always-QuickRun policy;
- Smart QuickRun enablement and explicit zero-, one-, and multiple-selection
  target choices from `IQuickRunCommandRegistry`;
- editable QuickRun, QuickUndo, and BetterSave global shortcuts; and
- immediate application of watcher, Songs-folder, and hotkey changes without
  restarting the desktop application.

`HotkeyEditor` is an Avalonia-native, two-way styled-property control. It
captures supported keyboard input, ignores modifier-only presses, clears a
binding with unmodified Delete, Backspace, or Escape, and translates Avalonia
key names to the numeric WPF `Key` values already stored by legacy settings.
This preserves settings compatibility while keeping WPF input types out of the
Application and Desktop presentation contracts.

The Smart QuickRun lists always include the legacy-compatible `<Current Tool>`
choice and otherwise use explicitly registered command display names filtered
by `QuickRunTargets`. Activating Preferences refreshes the lists so features
registered after application startup become available.

## BetterSave and live bindings

`BetterSaveService` is an Application-layer use case. It locates the current
beatmap, requires matching live editor state, and saves through
`IBeatmapEditingGateway`; consequently the existing mandatory backup-before-save
rule remains in force. The File menu, current-map context menu, global hotkey,
and automatic save override all delegate to this same service and receive typed
saved, unavailable, or failed outcomes.

`IHotkeyBindingCoordinator` lets Preferences replace process-wide shortcuts
without knowing about Windows hooks or hosted-service lifecycle. The existing
global-hotkey host owns all three callbacks and applies persisted bindings at
startup.

`WindowsBetterSaveOverrideService` isolates the Windows-only focused-process and
recursive filesystem-watcher behavior in Infrastructure. It observes `.osu`
changes only beneath the configured Songs folder, accepts only the path reported
as current by osu!, checks that an osu! window owns the foreground, serializes
callbacks, and suppresses the write event produced by BetterSave itself. A host
adapter applies persisted watcher settings at startup and stops observation at
shutdown. Unsupported platforms and invalid Songs folders fail through the
shared notification surface rather than preventing the rest of the application
from starting.

## Automated and build coverage

`Mapping_Tools.Platform.Tests` verifies persisted Preferences state, Smart
QuickRun filtering, live hotkey rebinding, watcher reconfiguration, legacy key
translation and display, hosted-service startup/shutdown, BetterSave menu
delegation, required-live opening, gateway saving, and typed warning/failure
outcomes. All 168 platform tests pass.

All 3 architecture tests pass. The Release solution build succeeds for Core,
Application, Infrastructure, Avalonia Desktop, the legacy WPF application,
both renderer tools, and test projects. Its 12 warnings are pre-existing legacy
package/SDK and nullable/analyzer warnings; the Avalonia projects introduced no
new build errors. Boundary searches find no WPF, WinForms, Avalonia, ReactiveUI,
or MVVM Toolkit references in Core or Application.

Per the user's explicit instruction, no PNG rendering, image comparison, native
interaction pass, or other visual-validation work was performed. Compilation
still validates the AXAML and compiled bindings.

## Deferred behavior

Wave 3 is complete. The shell now exercises every shared migration workflow,
but no individual mapping tool has yet been ported. Rhythm Guide remains Wave 4
step 21 and will be the first vertical feature slice to implement
`IShellProjectFeature`, multi-file editing, mandatory backup, typed project
persistence, and its auxiliary timing window.

## Documentation consulted

Avalonia 12.1 APIs and exact-version metadata used by this step:

- <https://docs.avaloniaui.net/docs/input-interaction/keyboard-and-hotkeys>
- <https://docs.avaloniaui.net/docs/custom-controls/defining-properties>
- <https://docs.avaloniaui.net/controls/input/selectors/combobox>
- <https://docs.avaloniaui.net/docs/how-to/combobox-how-to>
- <https://docs.avaloniaui.net/controls/input/selectors/checkbox>
- <https://docs.avaloniaui.net/controls/menus/menu>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>
- <https://www.nuget.org/packages/Avalonia/12.1.0>

The local Avalonia 12.1.0 reference assemblies were also checked for styled
property two-way binding, `SetCurrentValue`, keyboard modifiers, and the input
key names translated by `HotkeyEditor`.

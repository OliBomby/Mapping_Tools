# Wave 2 completion validation

Validated: 2026-07-25.

## Exit condition

Wave 2 requires a headless Application use case to select, load, edit, back
up, save, and report a map result without a `Window` or `UserControl`.

`Wave2CompletionTests.HeadlessQuickRunSelectsLoadsEditsBacksUpSavesAndReports`
exercises that chain through the production boundaries:

1. `BeatmapWorkspace` selects the current map through
   `ICurrentBeatmapLocator`.
2. `QuickRunService` dispatches the registry's current command without reading
   a view or dispatcher.
3. `ToolExecutionService` runs the command outside the initiating thread and
   owns its typed result.
4. `BeatmapEditingGateway` loads the selected fixture and applies a metadata
   edit.
5. The gateway requires `IBeatmapBackupService.CreateAsync` to complete before
   the text store receives its write.
6. The saved lines contain the edit and the execution service publishes a
   success notification.

The test uses only Application contracts and in-memory test adapters. No WPF,
Avalonia, dialog, process, or physical filesystem object participates.

## Step acceptance

| Step | Accepted capability | Validation evidence |
|---|---|---|
| 8 | File, folder, clipboard, launcher, and reveal ports | Adapter contract tests, cancellation and unavailable-platform tests |
| 9 | Settings and path ownership | Legacy fixture load/save and path-default tests |
| 10 | Typed project persistence | Compatibility fixtures, atomic-write and failure-preservation tests |
| 11 | Current-map workspace | Selection, recent history, picker, stale path, and live-locator tests |
| 12 | Editing gateway and Editor Reader adapter | Disk/live overlay, selected-object identity, save/reload ordering, and cancellation tests |
| 13 | Backup, restore, periodic policy, and QuickUndo | Mandatory pre-save backup, hashing, retention, restore safety, and QuickUndo tests |
| 14 | Tool execution and Generic Host | Progress, cancellation, typed outcomes, duplicate prevention, notification, reload, and shutdown tests |
| 15 | QuickRun registry, smart routing, and Windows hotkeys | Selection routing, stale state, failure, key conversion, hosted binding, and shutdown tests |

## Boundary audit

- `Mapping_Tools.Application` contains no WPF, WinForms, Avalonia, or
  ReactiveUI references.
- The Windows hook implementation is confined to Infrastructure behind
  `IGlobalHotkeyService`.
- The legacy WPF application remains buildable and available as the behavioral
  oracle.
- Public and protected APIs in non-legacy production projects pass the CS1591
  XML-documentation gate without suppressions.
- Wave 2 changed no AXAML; visual parity belongs to feature and shell steps
  beginning in Wave 3.

## Remaining work is outside Wave 2

Wave 3 owns the reusable Avalonia dialogs/forms, real shell navigation and
notification presentation, settings screens, current-map/project/backup UI,
hotkey editing and rebinding, BetterSave, and visible QuickRun controls.
Individual feature transformations begin in Wave 4 and register their
QuickRun commands as they migrate.

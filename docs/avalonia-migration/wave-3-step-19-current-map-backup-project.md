# Wave 3, step 19: current map, backup, and project lifecycle UI

Status: implemented, 2026-08-06.

## Scope delivered

The Avalonia shell now exposes the current-map and safety-copy workflows that
were extracted during Wave 2:

- startup restores the newest non-blank recent-map entry through
  `IBeatmapWorkspace`;
- the title-bar map surface displays selected filenames, the singular/plural
  selection count, and full paths in its tooltip;
- File-menu and current-map context-menu commands open one or many beatmaps,
  select the beatmap reported by osu!, generate explicit user backups, choose
  and restore a backup, and invoke QuickUndo;
- native file drops on the shell preserve item order and identify the
  selection as `BeatmapSelectionSource.DragAndDrop`;
- double-clicking selected rows in Get started's existing `TableView` installs
  those paths in table-selection order and identifies the source as recent
  history;
- recent rows refresh from the shared workspace whenever selection promotion
  changes persisted history; and
- the About menu opens the Mapping Tools data directory and configured backup
  directory through `IFileRevealService`.

`BeatmapWorkspaceViewModel` contains the presentation state and commands but
does not own a window, control, native storage item, or WPF compatibility
object. The view only translates Avalonia's file-drop payload into local paths
and translates the recent table's selected rows into typed presentation
models.

The existing WPF selection, backup, restore, and recent-map workflows remain
available. No legacy feature or compatibility facade was removed.

## Restore safety and outcomes

Restore is available only for one selected destination. The native picker is
limited to `.osu` and `.osb` files and starts in the configured backups
directory. A metadata mismatch is caught as
`BeatmapBackupIncompatibleException`; an owner-modal typed dialog displays the
backup and destination names and requires an explicit **Load anyway** choice
before retrying with `allowDifferentFilename`.

Successful backup and restore actions publish through the shared notification
surface. Picker cancellation is a no-op. Missing paths, unavailable live-map
lookup, platform rejection, and unexpected service failures produce warning
or error notifications without bypassing the Application-layer safety rules.
QuickUndo delegates to `IQuickUndoCommandService`, so the in-app action and
global hotkey use the same current-editor lookup, restore, reload, and outcome
logic.

## Project lifecycle shell contract

`IShellProjectFeature` is the Desktop boundary for feature-owned New, Open,
and Save operations. The Project menu is visible only while the active
feature implements that contract; commands delegate to that feature and route
uncaught errors to the shared notification surface. Get started and
Preferences are not savable and therefore do not show an empty Project menu.

The contract deliberately does not expose a project model, filesystem path,
serializer, view, or control. Each savable feature beginning with Rhythm Guide
will keep its typed `ProjectDefinition<TProject>`, `IProjectService` calls,
discard confirmation, loaded-state validation, and autosave targets beside
the feature that owns them.

## Automated and build coverage

`Mapping_Tools.Platform.Tests` verifies startup recent-map restoration and
shell text; ordered drag/drop selection; forced user-backup requests and
success notification; incompatible restore confirmation and explicit retry;
unavailable live-map feedback; ordered recent-row activation; and conditional
project-menu delegation. All 155 platform tests pass.

All 3 architecture tests pass. The Release solution build succeeds for Core,
Application, Infrastructure, Avalonia Desktop, the legacy WPF application,
both renderer tools, and test projects. The five remaining solution warnings
are pre-existing WPF SDK/package compatibility warnings; the affected
Avalonia Desktop build has zero warnings and zero errors. Boundary searches
find no WPF, WinForms, Avalonia, ReactiveUI, or MVVM Toolkit references in
Core or Application. Literal Avalonia colors remain confined to the existing
central `MappingToolsColors.axaml` dictionary.

Per the user's explicit instruction, no PNG rendering, image comparison, or
other visual-validation work was performed for this step. Compilation still
validates the AXAML and compiled bindings.

## Deferred behavior

BetterSave remains disabled in the File menu because it belongs to Wave 3
step 20 together with QuickRun hotkey editing, smart targets, live rebinding,
QuickUndo preferences, and the remaining Preferences controls. Feature-owned
project persistence becomes executable when the first savable Avalonia
feature, Rhythm Guide, implements `IShellProjectFeature` in Wave 4 step 21.

## Documentation consulted

Avalonia 12.1 APIs and exact-version metadata used by this step:

- <https://docs.avaloniaui.net/docs/data-binding/compiled-bindings>
- <https://docs.avaloniaui.net/controls/menus/menu>
- <https://docs.avaloniaui.net/api/avalonia/controls/menuitem>
- <https://docs.avaloniaui.net/docs/input-interaction/drag-and-drop>
- <https://docs.avaloniaui.net/docs/how-to/drag-and-drop-how-to>
- <https://docs.avaloniaui.net/docs/services/file-dialogs>
- <https://docs.avaloniaui.net/docs/services/storage/storage-provider>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>
- <https://www.nuget.org/packages/Avalonia/12.1.0>

The local Avalonia 12.1.0 reference assemblies were also checked for
`MenuItem.Command`, `DragEventArgs.DataTransfer`,
`DataTransferExtensions.TryGetFiles`, and `IStorageItem.TryGetLocalPath`.

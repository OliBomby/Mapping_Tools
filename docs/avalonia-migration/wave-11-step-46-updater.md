# Wave 11, step 46: I5 updater and updater UI

## Scope

Step 46 migrated the legacy updater use case and its user interface. The later
parity audit, executable switch, and legacy removal are recorded in steps 47–49.

The normative sources read for this migration were:

- `Mapping_Tools/Updater/UpdateManager.cs`
- `Mapping_Tools/Updater/UpdaterWindow.xaml` and `UpdaterWindow.xaml.cs`
- `Mapping_Tools/MainWindow.xaml` and `MainWindow.xaml.cs`
- `Mapping_Tools/ApplicationSettings.cs`
- Onova 2.6.2 package API documentation
- `.github/workflows/release.yml`, `installer/Mapping Tools*.iss`, and both frontend project files

## Delivered boundary

- `Mapping_Tools.Application/Updates/UpdateContracts.cs` contains the application update gateway and lifecycle contracts, release/check result models, persisted skip-version policy, shared preparation task, progress events, and launch state validation.
- `Mapping_Tools.Infrastructure/Updates/OnovaUpdateGateway.cs` owns GitHub/Onova integration, architecture-specific asset selection, release-note retrieval, ZIP extraction/staging, updater-process launch, and disposal.
- `Mapping_Tools.Desktop/Updates/UpdaterViewModel.cs` and `UpdaterInteractionService.cs` own decision/download windows, dispatcher marshaling, owner-modal shutdown progress, dialog/notification presentation, and restart/wait/skip interaction.
- `Mapping_Tools.Desktop/Views/UpdaterWindow.axaml` preserves the legacy updater copy, release-note panel, buttons, dimensions, progress range, logo, and download animation. The shell menu, startup check, and close lifecycle are connected through `MainViewModel` and `MainWindow`.

## Preserved legacy behavior

- Checks `OliBomby/Mapping_Tools` and selects `release_x64.zip` for a 64-bit process or `release.zip` otherwise.
- Uses the legacy latest-release endpoint for release title/body metadata and preserves the release title/body as optional values, including a `null` release response.
- Startup checks suppress versions at or below `ApplicationSettings.SkipVersion`; an explicit menu check ignores the skip setting. The exact manual messages remain `No new versions available.` and `Version {version} skipped because of user config.`.
- Preserves the three decisions and their effects: install now stages and launches with restart, install after closing shares the download and launches after shutdown without restart, and skip persists the offered version.
- Reuses one preparation task between the decision window and shutdown, reports normalized `0..1` progress, propagates cancellation/network/archive/staging failures, and retains the exact `UPDATER_EXCEPTION: ...` and `Error fetching update: ...` failure text.
- Leaves package download, ZIP extraction, staging, lock handling, replacement, rollback, permissions, and external process shutdown/restart to Onova, the same adapter used by the WPF implementation. The non-Windows gateway does not attempt to launch the Windows updater process.

The current release workflow publishes the Avalonia desktop application as
deterministically named Windows, Linux, and macOS runtime archives. The
historical `release.zip` and `release_x64.zip` aliases remain for the Windows
updater; the Windows-only updater and installer path does not claim to support
the portable Linux/macOS archives. The current release contract contains no
checksum or signature artifacts, so step 46 does not invent a new validation
format or claim validation behavior that the legacy package did not provide.

## Independent review follow-up

- Shared preparation is serialized and a new check observes the previous
  canceled/failed preparation before reusing the gateway. Stale preparation
  completions cannot mark a later check as staged, and the Wait and shutdown
  paths still observe the same task.
- Check and preparation cancellation is linked to service disposal; canceled
  lifecycle work is not presented as a user-facing updater error. Queued
  progress callbacks are ignored after the updater view model is disposed.
- The Avalonia executable now carries the same `1.12.30` assembly/file-version
  baseline as the legacy executable, and the release workflow keeps both
  project versions synchronized without changing the existing WPF ZIP asset
  layout. Switching the shipped executable remains step 48 scope.
- The updater window keeps the WPF-resizable 700x500 contract without adding
  new minimum dimensions, and the download label uses the Material foreground
  role corresponding to the legacy primary-dark foreground.

## Avalonia substitutions

The WPF updater code-behind and `MessageBox` calls are replaced by a typed `IDialogService`, an owner-modeless decision window, and an owner-modal shutdown progress window. `IUiDispatcher` replaces WPF dispatcher access. The legacy PNG/GIF assets are linked into the Avalonia resource set. These are framework substitutions only; labels, interaction order, timing branches, and messages remain normative WPF behavior.

## Verification

Focused tests cover update-channel/skip policy, preparation progress and launch mode, malformed/partial release metadata, updater view-model progress binding, and the Application/Infrastructure/Desktop boundary. The final verification run passed Application (149), Infrastructure (89), Desktop (207), and architecture (5) tests. Core, Application, Infrastructure, and Desktop production builds passed with zero errors; the WPF production build passed with zero errors and 15 existing warnings.

Avalonia references consulted during migration:

- https://docs.avaloniaui.net/docs/data-binding/compiled-bindings
- https://docs.avaloniaui.net/api/avalonia/controls/window
- https://docs.avaloniaui.net/docs/how-to/window-how-to
- https://docs.avaloniaui.net/docs/app-development/window-management
- https://docs.avaloniaui.net/controls/feedback/progressbar

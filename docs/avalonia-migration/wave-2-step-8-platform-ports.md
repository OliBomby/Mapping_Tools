# Wave 2, step 8: file and platform ports

Status: implemented, 2026-07-25.

## Scope delivered

The Application layer now owns framework-neutral contracts for:

- open-file, save-file, and folder pickers, including cancellation, capabilities, initial locations, multiple selection, and cross-platform file type metadata;
- text clipboard read, write, clear, and flush operations;
- URI, file, and folder launching;
- revealing a file or folder in the platform file manager;
- the legacy-compatible local application data and export directory layout.

`Mapping_Tools.Desktop` supplies Avalonia 12.1 adapters for storage pickers,
the clipboard, and the launcher. The adapters resolve their platform services
lazily from the main `TopLevel`, so constructing view models does not require
an initialized window and all missing-capability cases fail explicitly.

`Mapping_Tools.Infrastructure` supplies:

- a Windows-only Explorer adapter that preserves selection of a specific file;
- application directories rooted at
  `%LOCALAPPDATA%\Mapping Tools`, with exports at its `Exports` child.

The Avalonia composition root registers these adapters in
`DesktopPlatformServices`. The WPF `IOHelper`, `ShowSelectedInExplorer`, and
direct legacy call sites remain unchanged for migration compatibility.

## Behavior and limitations

- Picker cancellation returns an empty collection for open/folder pickers and
  `null` for save pickers.
- A caller cancellation token is checked before and after each native
  operation. Avalonia 12.1 picker APIs do not accept a token, so an already
  visible native dialog cannot be programmatically closed; a cancellation
  requested while it is open discards its result.
- Mapping Tools requires local filesystem paths. A storage item that cannot
  expose a local path fails with `IOException` rather than leaking an Avalonia
  storage object into Application.
- Exact file selection in a file manager is currently Windows-only. Other
  platform-specific reveal adapters can implement the same Application
  contract later.
- `ApplicationDirectories` preserves the legacy directory names but does not
  replace `MainWindow.AppDataPath`; that consumer migration belongs to Wave 2,
  step 9.

## Automated coverage

`Mapping_Tools.Platform.Tests` covers:

- filter normalization and all Avalonia filter metadata mappings;
- unavailable picker/clipboard services;
- picker and clipboard pre-cancellation;
- invalid or missing launcher targets;
- missing reveal targets without starting Explorer;
- compatibility of the application-data/export directory layout.

Native dialog appearance and operating-system handoff are not automatable in
the headless test run. They should be exercised through the first Avalonia
feature that consumes each port.

## Avalonia 12.1 sources consulted

- <https://docs.avaloniaui.net/docs/services/file-dialogs>
- <https://docs.avaloniaui.net/docs/services/storage/storage-provider>
- <https://docs.avaloniaui.net/docs/services/storage/file-picker-options>
- <https://docs.avaloniaui.net/docs/services/storage/storage-item>
- <https://docs.avaloniaui.net/docs/services/clipboard>
- <https://docs.avaloniaui.net/docs/services/launcher>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>
- Avalonia 12.1.0 reference metadata from the pinned NuGet package,
  specifically `IStorageProvider`, picker option types,
  `StorageProviderExtensions.TryGetLocalPath`, `IClipboard`, and `ILauncher`.

The launcher adapter uses the `TopLevel.Launcher` accessor and the
`ILauncher`/`LauncherExtensions` APIs supplied by the pinned Avalonia 12.1.0
packages.

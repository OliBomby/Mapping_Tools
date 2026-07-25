# Wave 2, step 9: settings and paths

Status: implemented, 2026-07-25.

## Scope delivered

The Application layer now owns a UI-independent settings document and
contracts for:

- loading, creating, and saving application settings;
- applying osu!, config, Songs, and backup path defaults;
- locating platform-specific settings inputs without exposing registry or
  filesystem details to Application;
- representing window bounds and hotkeys without WPF or Avalonia types.

`Mapping_Tools.Infrastructure` supplies a `System.Text.Json` settings store,
the legacy-compatible application directory layout, and the Windows
environment adapter that locates osu! through the registry and reads
`BeatmapDirectory` from the user config.

The Avalonia composition root registers the store, path environment, path
service, and settings service as DI singletons. The full .NET Generic Host
remains deferred to Wave 2 step 14, as recorded in the step 8 note.

The WPF frontend now uses a compatibility mapper between its bindable
`Settings` object and the framework-neutral document. Its existing preference
controls can therefore continue using WPF `Rect`, `Key`, and `ModifierKeys`
until that UI is migrated, while persistence and path policy no longer depend
on WPF. All consumers of `MainWindow.AppDataPath`, `MainWindow.AppCommon`, and
`MainWindow.ExportPath` now use the settings/application-directory boundary,
and those three globals have been removed.

## Legacy compatibility

- The configuration remains `%LOCALAPPDATA%\Mapping Tools\config.json`.
- Existing JSON property names are unchanged.
- Window bounds retain the Newtonsoft/WPF-compatible string representation
  `x,y,width,height`.
- Hotkey keys and modifiers remain numeric JSON properties.
- `TimeSpan` and skipped-version values retain their string representations.
- Null settings continue to be omitted when saving.
- New files persist clean defaults before machine-specific path defaults are
  applied, matching the legacy first-run sequence.
- Writes use a sibling temporary file followed by replacement, avoiding a
  partially written configuration file.
- Malformed JSON is rejected. The WPF bridge preserves its existing user
  notification and continues with in-memory defaults and derived paths rather
  than overwriting the corrupt file.

## Platform behavior and limitations

- Registry discovery is guarded as Windows-only. On other platforms, or when
  osu! cannot be located, the service uses `<local application data>\osu!`
  and reports that the fallback was used.
- A missing or unreadable osu! config falls back to the `Songs` directory.
- The backup directory is always created after path defaults are applied.
- The legacy WPF `SettingsManager` remains a static compatibility facade.
  Migrated Avalonia features should inject `ISettingsService` and
  `IApplicationDirectories` instead of using it.
- Preferences state, theme application, and global-hotkey activation remain
  feature migrations; this step establishes their persistence boundary only.

## Automated coverage

`Mapping_Tools.Platform.Tests` loads the versioned real-world legacy fixture
and verifies:

- recent maps, favorites, bounds, hotkeys, backup interval, and skipped
  version;
- a save/reload round trip with the legacy JSON value shapes intact;
- rejection of the corrupt JSON fixture;
- first-run defaults, fallback osu! paths, derived config/Songs/backup paths,
  directory creation, and the legacy first-save ordering;
- DI registration and legacy-compatible application directory paths.

This is a service-only migration step, so no visual baseline changed and no
new Avalonia UI API was introduced.

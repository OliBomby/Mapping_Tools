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
service, and settings service as DI singletons. Wave 2 step 14 subsequently
moved that composition root into the .NET Generic Host.

The WPF frontend now uses a compatibility mapper between its bindable
`Settings` object and the framework-neutral document. Its existing preference
controls can therefore continue using WPF `Rect`, `Key`, and `ModifierKeys`
until that UI is migrated, while persistence and path policy no longer depend
on WPF. All consumers of `MainWindow.AppDataPath`, `MainWindow.AppCommon`, and
`MainWindow.ExportPath` now use the settings/application-directory boundary,
and those three globals have been removed.

## Settings compatibility

- Legacy settings remain at `%LOCALAPPDATA%\Mapping Tools\config.json` and are
  never rewritten, so the legacy WPF tools can continue using them.
- New settings are written to `%LOCALAPPDATA%\Mapping Tools\preferences.json`.
- When `preferences.json` does not exist, the store reads `config.json` once and
  immediately writes the current model-shaped settings to `preferences.json`.
- Existing JSON property names are unchanged; canonical preferences follow the
  application settings model closely.
- Legacy window bounds retain the Newtonsoft/WPF-compatible string representation
  `x,y,width,height` while canonical preferences use the `WindowBounds` object.
- Hotkey keys and modifiers remain numeric JSON properties.
- `TimeSpan` and skipped-version values retain their string representations.
- Null settings continue to be omitted when saving.
- New files persist clean defaults before machine-specific path defaults are
  applied, matching the legacy first-run sequence.
- Writes use a sibling temporary file followed by replacement, avoiding a
  partially written preferences file.
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

`Mapping_Tools.Infrastructure.Tests` loads the versioned real-world legacy fixture
and verifies:

- recent maps, favorites, bounds, hotkeys, backup interval, and skipped
  version;
- immediate creation of canonical `preferences.json` without changing legacy
  `config.json`;
- rejection of the corrupt JSON fixture;
- first-run defaults, fallback osu! paths, derived config/Songs/backup paths,
  directory creation, and the legacy first-save ordering;
- DI registration and legacy-compatible application directory paths.

This is a service-only migration step, so no visual baseline changed and no
new Avalonia UI API was introduced.

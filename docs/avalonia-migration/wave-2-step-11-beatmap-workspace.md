# Wave 2, step 11: current beatmap workspace

Status: implemented, 2026-07-25.

## Scope delivered

The Application layer now owns current-map state through `IBeatmapWorkspace`.
The workspace provides:

- an ordered snapshot of zero, one, or many selected local paths;
- explicit selection sources for programmatic, startup, file-picker,
  recent-history, drag/drop, and live-editor changes;
- a notification carrying an immutable path snapshot after every explicit
  selection or clear operation;
- startup restoration from the newest recent entry;
- native `.osu`/`.osb` selection through the A8 `IFilePicker` port;
- missing-file reporting without silently removing or replacing selection;
- live osu! selection through `ICurrentBeatmapLocator`;
- removal and promotion of typed recent-map entries.

`BeatmapWorkspace` is independent of a Window, UserControl, WPF, Avalonia,
process APIs, and the physical filesystem. `IBeatmapFileSystem` owns existence
checks and parent-folder resolution. A `TimeProvider` supplies recent timestamps
so behavior is deterministic in tests.

## Recent-map compatibility

`ApplicationSettings.RecentMaps` now contains `RecentBeatmap` records instead
of unlabelled `string[]` values. Each record names its path and display date,
while `JsonSettingsStore` continues reading and writing the exact legacy JSON:

```json
[
  "C:\\path\\map.osu",
  "18/07/2026 17:38:50"
]
```

The display date deliberately remains text. Existing settings do not identify
the culture that formatted it, so parsing into `DateTime` would either reject
valid files from another locale or silently reinterpret day and month.

Behavior retained from the WPF implementation:

- selecting several paths preserves selection order but promotes each path
  individually, so the last selected path appears first in recent history;
- an existing exact path is removed before promotion;
- path comparison remains ordinal and case-sensitive;
- history is capped at 20 entries;
- startup recognizes the old pipe-joined multi-map entry shape and refreshes
  its selected paths with the current display timestamp;
- picker cancellation leaves selection and history untouched;
- a missing selected file is reported but remains selected.

The former empty-history startup bug, which created a recent entry containing
an empty path, is not retained.

## Frontend and platform integration

The Avalonia composition root registers the shared settings instance,
`TimeProvider`, physical filesystem adapter, current-beatmap locator, and
workspace as singletons. The temporary Avalonia shell does not yet expose the
current-map UI; that presentation work remains Wave 3 step 19.

Wave 2 step 12 replaced the temporary unavailable locator with the Windows
Editor Reader adapter. The workspace and editing gateway now share that one
singleton reader, so selecting osu!'s current map and opening its unsaved
version cannot disagree because of separate memory reads or path logic.

The WPF `MainWindow` now delegates selected paths, recent promotion, missing
path checks, startup restoration, and change notification to
`IBeatmapWorkspace`. Its existing public methods remain compatibility facades
for legacy tools. The WPF file-picker and live-current-map behavior are bridged
through WPF-side adapters, and its settings mapper translates typed recent
entries back to the bindable legacy list until that frontend is retired.

Legacy tools still calling `MainWindow.AppWindow.GetCurrentMaps()` are not
bulk-rewritten in this infrastructure step. Migrated feature view models must
inject `IBeatmapWorkspace`; the old calls disappear as each feature moves.

## Automated coverage

`Mapping_Tools.Platform.Tests` verifies:

- selection order, source notifications, and immutable snapshots;
- typed recent promotion, exact-path de-duplication, multi-map ordering, and
  the 20-entry limit;
- startup restoration of a pipe-joined legacy entry;
- no blank selection or recent entry for empty history;
- picker success, cancellation, single/multiple mode, file filters, current
  map parent folder, Songs fallback, and disabled start-folder preference;
- missing-file reporting without selection mutation;
- unavailable, stale, and successful live-current-map lookup outcomes;
- removing recent history without changing selection;
- loading and round-tripping the real Wave 0 settings fixture with the exact
  two-string recent-map JSON array;
- desktop DI registration and container validation.

The full solution builds with both WPF and Avalonia frontends. No AXAML or
visual state changed, so this step requires no render-baseline update.

## Avalonia 12.1 documentation

No new Avalonia API was introduced. The workspace reuses the A8 file-picker
port and adapter previously verified against:

- <https://docs.avaloniaui.net/docs/services/file-dialogs>
- <https://docs.avaloniaui.net/docs/services/storage/storage-provider>
- <https://docs.avaloniaui.net/docs/services/storage/file-picker-options>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>

Wave 2 step 14 subsequently moved the Avalonia composition root into the .NET
Generic Host.

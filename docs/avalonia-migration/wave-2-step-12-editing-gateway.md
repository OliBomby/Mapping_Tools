# Wave 2, step 12: beatmap editing and live osu! state

Status: implemented, 2026-07-25.

## Scope delivered

`IBeatmapEditingGateway` is now the Application-layer entry point for opening
and saving beatmaps and storyboards. It keeps two sources deliberately
separate:

- every beatmap starts from a complete file parsed through `ITextFileStore`;
- `ILiveBeatmapReader` may then supply the smaller set of values that
  Editor Reader can recover from osu!'s unsaved editor state;
- live values are applied only when the reconstructed live path is ordinally
  identical to the requested path;
- selected objects are returned as the same instances installed in the
  mutable beatmap, so a transformation can safely mutate the editor selection;
- storyboards remain disk-only because Editor Reader does not expose an
  equivalent unsaved storyboard model;
- saving is a separate operation from requesting an osu! reload.

`LiveBeatmapPreference` makes each use case explicit. `DiskOnly` never touches
process memory. `PreferLive` uses healthy matching state and otherwise returns
the disk document; it retains a read exception on the session when a failed
read caused that fallback. `RequireLive` reports disabled Editor Reader, no
active editor, a different open beatmap, invalid memory, or another read
failure rather than allowing a destructive tool to edit stale data.

The live overlay preserves the legacy `EditorReaderStuff.UpdateBeatmap`
behavior. It replaces bookmarks, timing points, and hit objects; updates
preview time, slider multiplier, and slider tick rate using invariant text;
sorts objects; and recalculates combo data, slider end times, and greenline
association.

## Windows infrastructure

`WindowsEditorReaderAdapter` is the only non-legacy project that references
`EditorReader.dll`. The vendor types end at that class: Application and Core
receive a `LiveBeatmapSnapshot` made from the existing beatmap-domain types.

The adapter:

- locates the stable `osu!.exe` process and confirms its main title ends in
  `.osu`;
- serializes access to the singleton, non-thread-safe Editor Reader instance;
- performs `FetchAll` with the legacy automatic de-stacking behavior;
- rejects impossible object values and inconsistent object/timing counts;
- converts timing effects, slider anchors, repeats, edge samples, custom
  samples, and selection flags with the same rules as the WPF implementation;
- reconstructs the current path beneath the configured Songs directory;
- writes `editor_reader_error.txt` beneath Mapping Tools application data when
  validation fails.

The same adapter implements `ICurrentBeatmapLocator` and is registered once in
desktop DI for both interfaces. A failed current-map lookup returns
unavailable, while a feature that requires live editing receives a meaningful
failure from the editing gateway.

`WindowsOsuEditorReloadService` replaces the legacy dependency on
`System.Windows.Forms.SendKeys`. It focuses osu! and sends the established ten
Ctrl+L gestures followed by Enter through Win32 keyboard input. The interface
is platform-neutral, but the implementation intentionally reports
`PlatformNotSupportedException` outside Windows. Save completes before reload
is requested, and cancellation is observed before saving and between the
focus and input phases.

The existing WPF `EditorReaderStuff` facade remains available for unmigrated
features. New and migrated use cases must inject `IBeatmapEditingGateway`;
rewriting every legacy tool in this infrastructure step would couple their
feature migrations together and remove the parity oracle prematurely.

## Automated coverage

`Mapping_Tools.Platform.Tests` verifies:

- disk-only opens never invoke the live reader;
- healthy state overlays only an exact matching path;
- another open difficulty falls back to the requested file;
- best-effort failures retain diagnostics while required-live failures throw;
- the disabled Editor Reader preference avoids process-memory access;
- cancellation before open avoids both disk and live reads;
- selected-object references are identical to objects in the mutable beatmap;
- live object sorting, bookmarks, preview time, and slider multiplier;
- save occurs before an optional editor reload;
- timing effect flags and bookmark conversion;
- slider anchor conversion, selection, repeats, and legacy edge-sample
  padding;
- invalid Reader counts are rejected after the legacy repair pass;
- desktop singleton registration and full container validation.

The focused platform suite passes 57 tests. Application and Infrastructure
build with the XML-documentation gate enabled. No AXAML or visual behavior
changed, so no render baseline applies.

## Deferred work and limitations

This step introduces the save/reload boundary but does not migrate BetterSave,
QuickUndo hotkeys, file-watcher save reconciliation, or individual tool call
sites. Wave 2 steps 13 and 14 subsequently supplied backup policy, shared tool
execution and interaction reporting, and the .NET Generic Host composition
root.

Editor Reader and the reload gesture support osu!stable on Windows only.
`FetchAll` is synchronous inside the vendor library: cancellation can prevent
or abandon a pending scheduled read, but cannot interrupt process-memory work
already executing.

## Avalonia 12.1 documentation

No Avalonia API was introduced or changed. This step affects Application
contracts, Windows Infrastructure adapters, and desktop DI registration only.

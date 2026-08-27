# Wave 9 step 42 — Hitsound Studio

Status: implemented in the current migration wave. This note covers step 42 only; Geometry Dashboard, updater work, parity audit, executable switching, and legacy removal remain later graph steps.

## Scope and source of truth

The WPF feature was read as the normative specification before implementation:

- `Mapping_Tools/Views/HitsoundStudio/HitsoundStudioView.xaml` and code-behind
- `Mapping_Tools/Views/HitsoundStudio/HitsoundLayerImportWindow.xaml`
- `Mapping_Tools/Views/HitsoundStudio/HitsoundStudioExportDialog.xaml`
- `Mapping_Tools/Viewmodels/HitsoundStudioVm.cs`
- `Mapping_Tools/Classes/HitsoundStuff/HitsoundImporter.cs`
- `Mapping_Tools/Classes/HitsoundStuff/HitsoundConverter.cs`
- `Mapping_Tools/Classes/HitsoundStuff/HitsoundExporter.cs`
- `Mapping_Tools/Classes/HitsoundStuff/MidiExporter.cs`
- `Mapping_Tools/Classes/HitsoundStuff/SampleImporter.cs`

The documented migration order was followed: layer/model editor, beatmap/sample import, playback, MIDI/SF2, effects/generation, then export dialog/package.

## Architecture

- `Mapping_Tools.Core/Tools/HitsoundStudio/HitsoundStudioEngine.cs` owns timestamp zipping, volume balancing, custom-index optimization, schema reuse/growth/conflict detection, deterministic sample naming, and standard/mania positions. It has no filesystem, audio-library, Avalonia, or WPF dependency.
- `Mapping_Tools.Application/HitsoundStudio/` owns project/schema-compatible contracts, import/reload workflows, MIDI and beatmap use cases, package/export orchestration, progress/cancellation, preview ports, and filesystem/reveal ports.
- `Mapping_Tools.Infrastructure/Audio/NaudioAudioClipMixer.cs` is the only new audio adapter; file access is provided by the shared `Files/PhysicalBeatmapsetFileSystem.cs`. Existing step-41 `IAudioGenerator`, `IAudioExporter`, `IMidiService`, `AudioPreviewService`, SoundFont renderer, effects, playback, and spectrum boundary are reused; no duplicate decoder or spectrum implementation was added.
- The neutral `MidiNote` contract carries optional instrument and note labels supplied by the NAudio adapter, so WPF's human-readable MIDI layer names are preserved without moving NAudio types into Application.
- `Mapping_Tools.Desktop/` owns the Avalonia layer editor, typed import/export windows, compiled bindings, source/folder pickers, project commands, QuickRun, selection, and playback-session disposal.

The WPF project remains present and runnable. The serializer redirects the old root type `Mapping_Tools.Viewmodels.HitsoundStudioVm` to `HitsoundStudioProject` and emits the same legacy root name on save. Core hitsound objects continue to use the existing compatibility binder, preserving `hsstudioproject.json` and nested `SampleSchema` data.

## Preserved behavior

The migrated path covers simple, stack, beatmap hitsound, storyboard, and MIDI imports; multi-source import; duplicate sample canonicalization; duplicate-time removal; wildcard stack coordinates; reload grouping; selected-layer editing and priority movement; source validation; generated/SoundFont preview; cancellation and session disposal; standard custom-index export with greenlines; coinciding-position export; storyboard export; MIDI output and greenline volume changes; default/mixed/sample-format export; previous-schema reuse and growth; output cleanup; map/package naming; QuickRun; project/autosave snapshots; and export-folder reveal.

The standard map writer intentionally keeps the WPF distinction between redlines and generated greenlines. Coinciding mode writes positioned named samples without greenlines, storyboard mode writes `StoryboardSoundSample` events, and MIDI output is emitted only when the legacy `ExportMap` option is enabled.

## Explicit platform substitutions

- WPF `BackgroundWorker` is replaced by the shared keyed `IToolExecutionService`; progress is reported at the same major phases and cancellation is cooperative through every import, generation, mix, encode, and save boundary.
- WPF `WasapiOut` ownership is replaced by step-41 `IAudioPlaybackSession`; the selected preview is stopped and disposed before another preview and when the view model is disposed.
- WPF `ListView/GridView` is represented by the shared Avalonia `MaterialGridListView`, preserving read-only rows, integrated headers, extended selection, resizable columns, and the editor/preview/reorder actions.
- WPF modal `DialogHost` surfaces are owner-modal Avalonia `Window` dialogs with typed result view models. Native source, sample, folder, and schema pickers remain behind Application picker/project ports.

The normative WPF view has no spectrum visual; the step-41 D7 spectrum service remains available through the shared audio boundary and was not duplicated or forced into this view.

## Verification

Focused fixtures/tests include:

- `Mapping_Tools.Core.Tests/Tools/HitsoundStudioEngineTests.cs`
- `Mapping_Tools.Application.Tests/HitsoundStudio/HitsoundStudioProjectTests.cs`
- legacy-root serializer tests in `Mapping_Tools.Infrastructure.Tests/MapsetMerger/LegacyProjectJsonSerializerTests.cs`

Verified locally:

- Core, Application, and Infrastructure builds
- Avalonia Desktop build (0 warnings with the machine-level licensing metadata available)
- focused Core, Application, Infrastructure, and Desktop tests
- legacy WPF frontend build
- architecture search confirming no NAudio/NVorbis/Ogg/MIDI-library types were added to Core, Application, or Desktop view models

The initial sandboxed Avalonia and WPF build attempts were blocked before compilation by access-denied user-local licensing/SDK metadata; elevated frontend builds completed successfully. The source compile was also independently checked with `UsedAvaloniaProducts=`.

Avalonia references consulted for the 12.1 implementation were the official binding validation and converter guidance, pointer/window APIs, Avalonia 12 breaking-change notes, and the 12.1.0 release/source authorities:

- https://docs.avaloniaui.net/docs/data-binding/binding-validation
- https://docs.avaloniaui.net/docs/data-binding/how-to-create-a-custom-data-binding-converter
- https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- https://docs.avaloniaui.net/controls/primitives/window
- https://docs.avaloniaui.net/docs/input-interaction/pointer
- https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.0

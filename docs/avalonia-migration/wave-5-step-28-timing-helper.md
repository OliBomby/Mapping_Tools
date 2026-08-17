# Wave 5, Step 28: Timing Helper

Status: implemented as a bounded Core/Application/Desktop vertical slice.

The migration preserves the legacy Timing Helper workflow:

- use hit objects, bookmarks, greenlines, and/or redlines as timing markers;
- deduplicate nearby markers using the configured millisecond leniency;
- infer BPM changes or use a fixed beat distance between markers;
- round BPM values to human-friendly integer, half, tenth, hundredth, or
  thousandth values when the marker tolerance allows it;
- keep the first redline while optionally removing later redlines;
- insert redlines with copied hit-sound state and the configured first-barline
  behavior;
- run against the workspace selection or the current osu! beatmap through
  QuickRun, with progress, cancellation, backup-safe saves, and editor reload
  on QuickRun success; and
- load and save `timinghelperproject.json` using the legacy
  `Mapping_Tools.Viewmodels.TimingHelperVm` JSON type alias.

The timing algorithm lives in `Mapping_Tools.Core`, map loading/saving and
execution orchestration live in `Mapping_Tools.Application`, and the Avalonia
view model owns form state, project persistence, and QuickRun routing. The WPF
feature remains unchanged and runnable.

Focused Core, Application, Infrastructure, and Desktop tests pass. Both
frontends are built as part of migration verification where the environment
allows it.

Avalonia migration references consulted:

- https://docs.avaloniaui.net/docs/data-binding/binding-validation
- https://docs.avaloniaui.net/docs/data-binding/compiled-bindings
- https://docs.avaloniaui.net/docs/data-binding/how-to-create-a-custom-data-binding-converter
- https://docs.avaloniaui.net/docs/data-binding/data-binding-syntax
- https://docs.avaloniaui.net/api/avalonia/data/updatesourcetrigger
- https://docs.avaloniaui.net/docs/xaml/directives
- https://docs.avaloniaui.net/docs/migration/wpf
- https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.0
- https://github.com/AvaloniaUI/Avalonia/tree/12.1.0
- https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty
- https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observablevalidator

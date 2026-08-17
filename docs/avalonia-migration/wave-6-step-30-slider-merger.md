# Wave 6, Step 30: Slider Merger

Status: implemented as a bounded Core/Application/Desktop vertical slice.

The migration preserves the legacy Slider Merger workflow:

- selected, bookmarked, time-code, and everything imports;
- Move and Linear joins, plus an explicit Bézier bridge mode;
- conversion of supported slider paths to Bézier control polygons;
- playable-end versus final-anchor matching;
- circle-to-slider, slider-to-circle, circle-to-circle, and slider-to-slider
  geometry with endpoint hitsound and sample-set semantics;
- linear-result cleanup when requested;
- backup-safe saves through the shared editing gateway, editor reload on
  QuickRun, progress, cancellation, and duplicate-run protection;
- workspace runs and current-editor QuickRun; and
- `slidermergerproject.json` persistence using the legacy
  `Mapping_Tools.Viewmodels.SliderMergerVm` JSON type alias.

The geometric transformation lives in `Mapping_Tools.Core`, map loading,
selection, and backup/reload orchestration live in `Mapping_Tools.Application`,
and the Avalonia view model owns form state, project lifecycle, and QuickRun
routing. The WPF implementation remains unchanged and runnable.

Focused Core, Application, Infrastructure, and Avalonia Desktop tests cover the
new engine, import routing, view-model state, dependency registration, and
legacy project round trips.

Avalonia migration references consulted:

- https://docs.avaloniaui.net/docs/data-binding/binding-validation
- https://docs.avaloniaui.net/docs/data-binding/how-to-create-a-custom-data-binding-converter
- https://docs.avaloniaui.net/docs/migration/wpf
- https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.0
- https://github.com/AvaloniaUI/Avalonia/tree/12.1.0
- https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/generators/observableproperty
- https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/generators/relaycommand

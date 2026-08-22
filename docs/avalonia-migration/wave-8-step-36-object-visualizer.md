# Wave 8, step 36: D5 object visualizer

## Scope

The object visualizer is a presentation-only feature. `Mapping_Tools.Desktop`
owns the Avalonia control, marker type, slider-path preparation, bounds fitting,
drawing, and thumbnail rendering. The control receives Core `HitObject` data
directly; Application and Core do not expose scene, viewport, hit-test, or
visualizer-specific model types.

The main `ObjectVisualiserControl` is intentionally close to the legacy WPF
`HitObjectElement`: it draws one circle or slider, supports an optional custom
slider length, progress-ball animation, slider anchors, and extra slider
markers. It does not own selection, hover, panning, zooming, generic scene
composition, combo labels, follow lines, or spinner rendering.

Pattern Gallery uses a separate Desktop-only `PatternThumbnailControl`, based
on the legacy `OsuPatternToThumbnailConverter`. Its Application service returns
the loaded domain `Beatmap`; thumbnail layout and drawing stay in Desktop.

## Intentional platform substitution

The WPF `HitObjectElement : FrameworkElement` is represented by
`ObjectVisualiserControl : Avalonia.Controls.Control`. Avalonia has no WPF-style
element template for this path/anchor drawing contract, so the control overrides
`Render(DrawingContext)` and performs the same direct drawing and size-to-fit
calculation. Avalonia's logical DIPs are used directly as the control's drawing
coordinates.

## Verification

- Desktop tests cover the legacy defaults, one-object input contract, and path
  safety limits.
- Application/Core visualizer classes and tests were removed.
- The Avalonia Desktop project builds successfully.

Avalonia 12.1 references consulted:

- https://docs.avaloniaui.net/docs/custom-controls/custom-control-class
- https://docs.avaloniaui.net/docs/custom-controls/drawing-custom-controls
- https://docs.avaloniaui.net/docs/input-interaction/pointer
- https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.0
- https://www.nuget.org/packages/Avalonia/12.1.0

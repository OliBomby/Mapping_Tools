# Wave 8, step 36: D5 object visualizer

## Scope

This step extracts reusable object scene data, slider polyline geometry, osu!
coordinate transforms, fit/zoom/pan calculations, and deterministic object and
anchor hit testing. `Mapping_Tools.Desktop` owns the Avalonia brushes, custom
drawing, clipping, pointer capture, selection, hover state, and pointer-wheel
zoom boundary.

Pattern Gallery, thumbnail bitmap conversion, combo-number rendering, and
feature-specific placement remain step 37 scope.

## Intentional platform substitution

The WPF `HitObjectElement : FrameworkElement` is represented by
`ObjectVisualiserControl : Avalonia.Controls.Control`. Avalonia has no
WPF-style element template that provides this path/anchor drawing contract, so
the control overrides `Render(DrawingContext)` and uses `PushClip` while
drawing the same object diameter, fractional outline, endpoint, slider-ball,
anchor, duplicate-anchor, and marker semantics. The WPF size-to-fit behavior is
represented by the framework-neutral `ObjectVisualiserTransform` and the
control's `FitToScene` method; fit mode is reapplied after a resize while a
user pan or zoom remains stable. Avalonia drawing and pointer coordinates are
logical DIPs, so no platform-specific DPI conversion is required. Middle-button
capture pans and the wheel zooms around the pointer; left-button selection and
pointer hover stay in the control boundary so later tools can subscribe without
owning Avalonia input types. Avalonia's `PointerCaptureLost` callback clears an
interrupted pan, and marker collections are observed while attached so in-place
updates invalidate the control like the WPF observable collection.

## Verification

- Core: five focused transform/path tests.
- Application: six focused scene/hit-test tests.
- Desktop: five focused control tests; the full Desktop suite passes (161 tests).
- Legacy WPF build and Avalonia Desktop build both pass.

Avalonia 12.1 references consulted:

- https://docs.avaloniaui.net/docs/custom-controls/custom-control-class
- https://docs.avaloniaui.net/docs/custom-controls/drawing-custom-controls
- https://docs.avaloniaui.net/docs/input-interaction/pointer
- https://raw.githubusercontent.com/AvaloniaUI/Avalonia/12.1.0/src/Avalonia.Base/Input/PointerEventArgs.cs
- https://raw.githubusercontent.com/AvaloniaUI/Avalonia/12.1.0/src/Avalonia.Base/Input/InputElement.cs
- https://raw.githubusercontent.com/AvaloniaUI/Avalonia/12.1.0/src/Avalonia.Base/Visual.cs
- https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.0

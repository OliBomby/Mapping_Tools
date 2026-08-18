# Wave 8, step 38: D6 graph/value editor

## Scope

This step extracts the graph subsystem's framework-neutral interpolation
contracts, built-in interpolator math, graph state, marker generation, and
scalar-or-anchor text format into `Mapping_Tools.Core`. `Mapping_Tools.Desktop`
owns the Avalonia custom drawing, pointer capture, context menu, snapping,
keyboard/focus boundary, typed-value dialog, and the reusable
`ValueOrGraphControl` surface.

Sliderator, Tumour Generator, audio integration, Windows runtime work, and
release cutover remain deferred to steps 39 and later. The legacy WPF graph and
its consumers are intentionally unchanged.

## Parity decisions

- A default graph is a unit-bounded pair of centered edge anchors, matching the
  WPF `Graph` constructor.
- The persisted interpolator order and scalar-or-pipe-separated-anchor text
  format remain stable. Invalid text is reported as a binding validation error
  instead of throwing from the legacy converter's short-anchor path.
- Anchor movement preserves edge locking, neighbor ordering, Y bounds, Shift Y
  locking, Ctrl X locking, Alt snap bypass, marker snapping, non-edge deletion,
  interpolation context-menu selection, and typed value editing.
- Tension dragging preserves the WPF 200-pixel scale, Ctrl precision modifier,
  and vertical-mirror handling. Pointer capture loss, Escape, Delete/Backspace,
  focus acquisition, pan, and wheel zoom are handled by the Avalonia control.
- WPF cursor warping after a drag is omitted: Avalonia 12.1 exposes pointer
  capture but no portable equivalent for repositioning the OS cursor. The
  gesture remains pointer-relative and continues across the control boundary.
- WPF `Freezable` graph snapshots become plain Core cloneable state. Dialog
  editing starts from a clone and only publishes a clone after OK, so Cancel
  cannot mutate the host value.
- WPF `DialogHost` is represented by a Desktop-owned modal Avalonia `Window`;
  the graph/value behavior remains reusable and no feature-specific consumer is
  added in this step.

## Verification

- Core: focused graph tests pass, including interpolation evaluation,
  derivatives/integrals, default/empty state behavior, text validation,
  catalog ordering, cloning, and marker generation.
- Desktop: focused graph-control and converter tests pass, including default
  state, constraints/modifiers, snapping, add/remove rules, viewport mapping,
  zoom focus, and scalar/graph conversion. The full Desktop suite passes (173
  tests).
- Avalonia Desktop build passes with `UsedAvaloniaProducts=` to bypass the
  sandbox-blocked Avalonia licensing-ticket scan; the only reported warning is
  the pre-existing `TextBox.Watermark` warning in Pattern Gallery.
- Legacy WPF build passes. Its pre-existing package/platform/analyzer warnings
  remain; no WPF source was changed.

Avalonia 12.1 references consulted:

- [Custom control class](https://docs.avaloniaui.net/docs/custom-controls/custom-control-class)
- [Drawing custom controls](https://docs.avaloniaui.net/docs/custom-controls/drawing-custom-controls)
- [Custom rendering](https://docs.avaloniaui.net/docs/graphics-animation/custom-rendering)
- [Input events](https://docs.avaloniaui.net/docs/events/input-events)
- [Focus](https://docs.avaloniaui.net/docs/input-interaction/focus)
- [Context menus](https://docs.avaloniaui.net/controls/menus/contextmenu)
- [Avalonia 12.1 breaking changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)

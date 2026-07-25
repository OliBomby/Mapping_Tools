# Control and interaction parity

Read this reference before choosing controls or styles for any migrated UI.
The legacy WPF view is both a visual and behavioral specification unless the
user explicitly requests a redesign.

## Inventory before implementation

Inspect the real WPF XAML, templates, styles, code-behind, commands, and the
view inside its shell. For every visible element, record:

- its semantic control type and item/container hierarchy;
- commands, pointer gestures, keyboard access, selection, focus, hover,
  checked, pressed, disabled, and validation behavior;
- draggable window regions, movable splitters, resizable columns, scrolling,
  clipping, overflow, context menus, and popup placement;
- empty, populated, selected, focused, checked, unchecked, hovered, and
  open-menu states;
- theme variants, icon source, shadow depth, borders, density, typography,
  padding, and content-dependent sizing.

Do not infer behavior from a screenshot alone. Inspect the WPF implementation
and exercise the legacy application.

## Preserve the semantic control

Choose the Avalonia control that owns the same behavior. Styling a substitute
until it resembles the reference is not parity.

- Map menus and context menus to `Menu`, `MenuItem`, and `ContextMenu`.
- Map selectable lists to `ListBox`/`ListBoxItem`, not stacked text blocks.
- For a read-only multi-column list in Avalonia 12.1, prefer `TableView` when
  it supplies the required integrated headers, rows, selection, scrolling,
  and `CanUserResizeColumns`. Do not build headers and rows as unrelated
  grids. Verify both empty and populated states and drag a real column
  resizer.
- Map movable WPF grid separators to `GridSplitter`, preserving resize
  direction, bounds, cursor, and hit target.
- Use Material.Avalonia `ColorZone` where the legacy view uses a material
  color surface. Its default shadow may differ; set `ShadowAssist.ShadowDepth`
  explicitly, including `Depth0` for flat nested zones.
- Use the Material outlined `TextBox` variant for an outlined field. Do not
  draw a separate decorative border.
- Use an Avalonia-12-compatible icon pack for icons. Do not replace icons with
  arbitrary text glyphs. Confirm the exact package major version; packages
  built for an earlier Avalonia major can compile and fail at runtime.

If Avalonia lacks an exact counterpart, document the mismatch before creating
a custom control and reproduce the full behavior, not just its resting pixels.

## Inspect exact control templates

Read the templates from the exact Material.Avalonia and Avalonia 12.1 package
or tagged source before overriding them.

- Theme controls have minimum heights, internal borders, floating-label
  panels, presenters, and named template parts. Hard-capping the outer control
  below its themed minimum can clip one edge of an outline. Apply a narrowly
  scoped class and override the responsible named template parts instead.
- Material defaults are not legacy density defaults. For example, popup menu
  items can be much taller than WPF items. Compare an opened menu and an
  opened context menu, then scope compact item height, font, icon size, and
  popup presenter margins without changing top-level menu geometry.
- A `TableView` column with `Auto` in Avalonia 12.1 does not necessarily have
  WPF `GridViewColumn.Width = Double.NaN` content-sizing behavior. Inspect the
  exact layout implementation. When needed, measure initial content in the
  view, apply a pixel width after attachment, invalidate both header and cell
  presenters, and leave user resizing enabled afterward.
- Headers and cells may have different legacy padding even though their column
  boundaries are shared. Compare both boundaries and text origins with
  populated data.

Keep selector placement valid:

- Put descendant selectors that target controls outside a template in normal
  `Styles`.
- Inside a `ControlTheme`, use supported nested theme/template selectors such
  as `^` and `/template/`.
- Do not place an arbitrary descendant selector directly inside a
  `ControlTheme`; compiled AXAML can still fail only when the view is rendered.

## Custom chrome and state feedback

Custom title bars require explicit behavior.

- An interactive region marked as application content will no longer inherit
  native title-bar dragging. On a non-button region, handle a left-button
  press and call `Window.BeginMoveDrag`.
- Keep buttons and menus excluded from the drag gesture and preserve any
  context menu on the drag surface.
- Explicitly set intended shadow depth on nested color surfaces.
- Give toggles visible checked and unchecked states with pseudo-class styles.
  For a navigation toggle, icon brightness may communicate the state even
  when geometry does not change.
- Verify minimize, maximize/restore, close, title drag, double-click behavior
  where required, context menus, and hover/pressed feedback in a real desktop
  run.

## Typography

Use the same font face and size before judging weight. When Avalonia text looks
dimmer than WPF, first match the foreground color. Increasing `FontWeight` can
make the result visibly too thick because weight mapping and rasterization
differ between renderers. Ignore only unavoidable subpixel rasterization after
face, size, foreground, weight, line height, spacing, and clipping match.

## Required evidence

For each migrated view:

1. Render WPF and Avalonia at the same viewport and deterministic state.
2. Render at least empty and representative populated/overflow states.
3. Open both images and compare layout, content boundaries, and typography.
4. Capture or inspect opened menu/context-menu states when present.
5. Exercise hover, focus, selection, checked state, column resizing, splitter
   movement, scrolling, and keyboard access as applicable.
6. Use a native desktop run for title dragging, window controls, platform
   dialogs, popup behavior, and anything a headless renderer cannot prove.
7. Treat compilation as an implementation check, never as visual or
   interaction evidence.

Do not report completion while a semantic substitute, inactive chrome,
missing state feedback, clipped themed control, or obvious application-owned
visual mismatch remains.

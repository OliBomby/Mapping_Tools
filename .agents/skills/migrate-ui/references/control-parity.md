# Control and interaction parity

Read this reference before choosing controls or styles for any migrated UI.
The legacy WPF view is the source, visual, and behavioral specification unless
the user explicitly requests a redesign. Start from that XAML and keep a
minimal structural diff; matching a resting screenshot after rewriting the
control tree is not parity.

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

## Concrete WPF-to-Avalonia mapping catalog

Use these project-owned mappings before introducing view-local templates or a
new control. The Avalonia component replaces only the reusable presentation
and interaction contract; feature-owned bindings, commands, columns, tooltips,
context menus, gestures, and validation remain in the migrated view.

| WPF source                                                      | Avalonia mapping                                                | Project usage                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
|-----------------------------------------------------------------|-----------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| MaterialDesignInXaml `ListView` without `ListView.View`         | `ListBox Classes="material-list-view"`                          | The application-level styles in `Mapping_Tools.Desktop/Resources/Styles/MaterialListBoxes.axaml` preserve the compact item density, transparent surface, hover treatment, and horizontal scrollbar behavior. Keep `ItemsSource`, item templates, selection behavior, and scroll visibility on the view instance.                                                                                                                                                                               |
| MaterialDesignInXaml `ListView` with `ListView.View = GridView` | `controls:MaterialGridListView` with `TableViewColumn` children | The control and its co-located theme in `Mapping_Tools.Desktop/Controls/MaterialGridListView.*` preserve integrated headers, row/cell density, dividers, hover/selection feedback, scrolling, and column-resizer hit targets. Keep `CanUserResizeColumns`, selection mode, column definitions, bindings, gestures, context menus, and feature-specific initial column measurement in the view. Avalonia 12.1 `Auto` width is not assumed to reproduce WPF `GridViewColumn.Width = Double.NaN`. |
| Selectable WPF list                                             | `ListBox` and `ListBoxItem`                                     | Preserve keyboard focus, selection mode, selected state, and item-container behavior; do not replace it with an `ItemsControl` or stacked panels.                                                                                                                                                                                                                                                                                                                                              |
| WPF `GridSplitter`                                              | Avalonia `GridSplitter`                                         | Preserve orientation, resize direction and behavior, bounds, cursor, alignment, and hit target.                                                                                                                                                                                                                                                                                                                                                                                                |
| MaterialDesignInXaml `ColorZone`                                | Material.Avalonia `ColorZone`                                   | Set `ShadowAssist.ShadowDepth` explicitly, including `Depth0` for flat nested zones.                                                                                                                                                                                                                                                                                                                                                                                                           |
| `HintAssist.Hint` plus `MaterialDesignFloatingHintTextBox`      | Material.Avalonia `TextBox` with `TextFieldAssist.Label`        | Reuse the shared text-field styles; keep typed conversion and validation in bindings and `INotifyDataErrorInfo`.                                                                                                                                                                                                                                                                                                                                                                               |
| WPF `Menu`/`MenuItem`/`ContextMenu`                             | Avalonia `Menu`/`MenuItem`/`ContextMenu`                        | Preserve commands, input gestures, icons, opening behavior, placement, enabled state, and compact popup density.                                                                                                                                                                                                                                                                                                                                                                               |
| Legacy tool title/help/QuickRun header                          | `ToolViewHeader`                                                | Keep the legacy title, description, badges, and help interaction; do not rebuild the header per view.                                                                                                                                                                                                                                                                                                                                                                                          |
| Legacy single-run button and progress surface                   | `ToolRunButton` and `ToolProgressBar`                           | Bind the feature execution state and command while preserving the legacy placement and lifecycle behavior.                                                                                                                                                                                                                                                                                                                                                                                     |
| `ToggleButton` with style `MaterialDesignSwitchToggleButton`    | `ToggleSwitch`                                                  |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| Italic `TextBlock`                                              | Same                                                            | Add 3 pixels of padding on the right side to prevent the last letter getting clipped.                                                                                                                                                                                                                                                                                                                                                                                                          |
| `TextBox` without `materialAssists:TextFieldAssist.Label`       | `TextBox` with `Classes="compact"` | We need the compact style to match the reduced field height in WPF.                                                                                                                                                                                                                                                                                                                                                                                                                            |

## Material palette discipline

Treat Material.Avalonia as the canonical palette. Use `DynamicResource` so
runtime light/dark changes propagate. Prefer these exact 3.17.0 keys:

- `MaterialPrimaryMidBrush`, `MaterialPrimaryLightBrush`, and
  `MaterialPrimaryDarkBrush` for the configured primary swatch;
- `MaterialPrimaryMidForegroundBrush` for content on the mid-primary surface;
- `MaterialPaperBrush`, `MaterialCardBackgroundBrush`, `MaterialBodyBrush`,
  and `MaterialBodyLightBrush` for surfaces and text;
- `MaterialDividerBrush`, `MaterialSelectionBrush`, and
  `MaterialDataGridRowHoverBackgroundBrush` for standard state feedback;
- `MaterialValidationErrorBrush` for invalid state; and
- `MaterialFlatButtonClickBrush`, `MaterialSnackbarBackgroundBrush`, and
  `ShadowAssist.ShadowDepth` instead of custom button, notification, or
  shadow colors.

Do not assume a primary foreground is white: Material computes it for
contrast, and Blue 500's mid-foreground may be black. When legacy chrome
requires white content on blue, use an existing Material light-on-dark brush
such as `MaterialDarkForegroundBrush` after verifying contrast.

Do not put hexadecimal colors in views. If a visual role genuinely has no
Material equivalent, add one semantic resource to the small centralized
`Mapping_Tools.Desktop/Resources/MappingToolsColors.axaml` dictionary and use
it everywhere. Before handoff, search:

```powershell
rg -n --glob "*.axaml" "#[0-9A-Fa-f]{3,8}" Mapping_Tools.Desktop
```

Only the centralized custom-color dictionary should match.

## Style ownership

- Keep a custom control's styles in its AXAML or a co-located control-owned
  style file.
- Keep view-only and shell-only styles in the owning view or shell.
- Put application-wide Material compatibility overrides in focused files under
  `Mapping_Tools.Desktop/Resources/Styles` and include them from `App.axaml`.
- Keep `App.axaml` as the composition root for themes, global resources, and
  style includes. Do not define unrelated component styles inline there.
- Prefer an explicit style class when an override applies only to a legacy
  control variant. Use a type-wide selector only for genuinely universal
  application behavior.

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

### Floating-label text fields

- Map WPF `HintAssist.Hint` plus `MaterialDesignFloatingHintTextBox` to a real
  Material.Avalonia `TextBox` with `TextFieldAssist.Label`. The label is part
  of the control template; never add a sibling `TextBlock` above it.
- Reuse the application-level non-outlined `TextBox` styles composed by
  `App.axaml` from `Resources/Styles/TextBoxes.axaml`.
  Do not add per-view `Height`, `MinHeight`, dense classes, label margins, or
  underline offsets unless the exact template and a native reference prove a
  distinct control variant is required.
- Apply the shared `compact` class to text fields and combo boxes that do not
  have a Material label. Labeled fields use the standard 40-pixel layout;
  label-less fields use the compact 26-pixel layout and centered content.
- In Material.Avalonia 3.17.0, the useful template parts include
  `PART_TextFieldPanel`, `PART_LabelRootBorder`, `PART_LabelText`,
  `PART_DataValidation`, and `PART_ErrorPresenter`. Adjust the responsible
  internal part, not a compensating outer margin.
- The default text-field panel is 56 pixels high and the dense variant is 48.
  Neither automatically matches the legacy layout. Judge label-to-value,
  value-to-underline, and row-to-row spacing separately before changing
  geometry.
- Material's `TextBox` template already wraps the field in
  `DataValidationErrors` and presents correction text through
  `PART_ErrorPresenter`. Never add a separate validation `TextBlock`; it
  creates a second layout row and breaks field ownership.

Keep selector placement valid:

- Put descendant selectors that target controls outside a template in normal
  `Styles`.
- Inside a `ControlTheme`, use supported nested theme/template selectors such
  as `^` and `/template/`.
- Do not place an arbitrary descendant selector directly inside a
  `ControlTheme`; compiled AXAML can still fail only when the view is rendered.

## Typed text conversion

Keep bindable values typed. An `int`, `double`, enum, date, duration, or other
non-string value must not acquire a parallel `FooText` property merely because
a `TextBox` edits it.

- Put reusable two-way `IValueConverter` implementations in the frontend's
  shared converter location. Convert typed values to presentation text in
  `Convert` and parse edits in `ConvertBack`; never move this work into a view
  model setter.
- Reuse converters by type and formatting semantics rather than creating one
  converter per field. Expose a shared instance through application resources
  or a static property.
- Choose culture and number styles deliberately. Use the legacy/persistence
  format when parity requires it; otherwise use the intended UI culture.
- Report empty or malformed input as a binding validation failure. Never
  silently coerce invalid text to `0`, another default, or the previous value.
  The invalid edit must remain visible for correction.
- Test forward conversion, valid conversion back, empty/null input, malformed
  input, boundary values, and culture-sensitive input.

## Validation presentation and ownership

Avalonia does not provide WPF's XAML
`Binding.ValidationRules`/`ValidationRule` collection. Define validation rules
with `System.ComponentModel.DataAnnotations` on the typed bindable property and
let Avalonia consume the resulting `INotifyDataErrorInfo` errors.

- Use built-in attributes such as `Required`, `Range`, and `StringLength`
  whenever they express a rule already present in the legacy or domain
  contract. Never invent a new range or required constraint during migration.
- For domain-specific or cross-property rules, create a reusable custom
  `ValidationAttribute`, override `IsValid(object?, ValidationContext)`, and
  return a meaningful `ValidationResult`. Put UI-independent attributes in
  Core or Application according to the project boundaries.
- Derive each validating view model directly from CommunityToolkit.Mvvm's
  `ObservableValidator` and use `[NotifyDataErrorInfo]` on generated properties.
- Run property validation when the value changes. Validate all properties
  before submit/save and block persistence while any error remains.
- Keep persistence gating outside the view. Invalid edits must not reach
  shared settings or trigger a save.
- Do not expose parallel `FooError` and `HasFooError` properties unless a
  non-validation consumer actually needs them.
- Bind editable values `TwoWay`. Route converter failures and annotation
  errors through Avalonia's binding-validation pipeline, and let the Material
  text-field template own the error label, underline, spacing, and correction
  presentation instead of adding a separate error control.

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

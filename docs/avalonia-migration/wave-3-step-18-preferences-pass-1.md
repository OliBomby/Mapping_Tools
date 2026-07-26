# Wave 3, step 18: Preferences, pass 1

Status: implemented, 2026-07-26.

## Scope delivered

Preferences is now an explicit, lazy shell feature with its own compiled
`PreferencesView` and ReactiveUI `PreferencesViewModel`. This first pass
preserves the legacy controls and behavior for:

- the osu!, Songs, osu! user configuration, and backups paths;
- native single-folder pickers for directory fields and a filtered native
  single-file picker for `osu!.*.cfg`;
- the retained-backup limit;
- automatic and periodic backups, including the periodic interval;
- using the current beatmap as the default folder in general tool pickers;
- enabling or disabling Editor Reader; and
- switching the complete application between light and dark palettes.

The view keeps the legacy blue 32-point heading, floating field labels,
underline text fields, folder icon buttons, checkbox order and density, and
Light/toggle/Dark presentation. It uses real Avalonia `TextBox`, `Button`,
`CheckBox`, and `ToggleSwitch` controls rather than pointer-driven visual
facades. It scrolls in both directions when the shell is narrower or shorter
than the legacy minimum.

Like the WPF `HintAssist.Hint` fields, every caption belongs to the Material
text-field template. The Avalonia view supplies it through
`TextFieldAssist.Label`; it does not place a separate `TextBlock` above a
text box. A shared application-level default for every non-outlined text box
reduces the internal panel to 40 pixels, keeps floating captions gray except
during validation, moves captions away from entered text, and moves entered
text closer to the underline. Preferences uses eight-pixel gaps between
these fields, producing the same 48-pixel line rhythm as the legacy view.

The same shared field style is also used by the reusable value-entry dialog
and automatically applies to ordinary text boxes in future views. The shell
search field remains intentionally separate because it uses an outlined
search treatment.

QuickRun hotkeys and smart targets, QuickUndo, BetterSave, AutoReload,
Always QuickRun, and the BetterSave override remain Wave 3 step 20. They are
not displayed as disabled or non-functional placeholders in this pass.

## Validation and persistence

Every valid edit updates the process-lifetime `ApplicationSettings` instance
used by backup, workspace, and Editor Reader services, then immediately saves
the complete legacy-compatible JSON document through `ISettingsService`.
There is no view code-behind persistence or static WPF settings dependency.

Path fields reject blank values without replacing the last usable setting.
The retained-backup limit accepts 1 through 100000. The periodic interval uses
the invariant constant format, displays `hh:mm:ss` for ordinary intervals,
and rejects malformed values and intervals below one second. Invalid edits
remain visible for correction and do not reach the shared settings document.
The view models expose corrections through `INotifyDataErrorInfo`, allowing
Material.Avalonia's own `DataValidationErrors` element inside each text-field
template to render the red caption, underline, and explanation. There are no
separate validation text blocks in Preferences or the reusable value dialog.

Picker cancellation leaves both the field and persisted document unchanged.
Picker and save failures are caught by the view model and published through
the shared notification surface. A failed save leaves the valid change active
for the current process and tells the user that `config.json` was not updated.

## Theme compatibility

`ApplicationSettings.Theme` persists the palette as `Dark` or `Light`.
Documents created before step 18 omit that property and therefore retain the
legacy dark default. The WPF compatibility model and mapper preserve the new
property, so opening and saving settings through the legacy frontend does not
silently discard an Avalonia theme choice.

Startup applies the saved variant before constructing the main window.
Runtime changes update both Avalonia's requested theme variant and
Material.Avalonia's exact-version `BaseThemeMode`. Theme dictionaries supply
the shell, navigation, migrated content, dividers, selection feedback, and
table rules with matching light and dark resources.

## Automated and visual coverage

`Mapping_Tools.Platform.Tests` verifies initial presentation without a write;
valid and blank paths; valid and invalid backup limits and intervals; live
periodic-policy changes; light-theme application and persistence; selected
folders; cancelled config selection; legacy/default settings loading; theme
JSON round trips; DI registration; and the existing shell behavior. All 132
platform tests pass. All 3 architecture tests pass.

Both frontends build. The Avalonia Release project builds with zero warnings
and zero errors. The WPF project builds with its existing SDK/package
compatibility warnings and no errors.

The following captures were opened and inspected:

- `artifacts/view-renders/wave3-step18-preferences-wpf.png`
- `artifacts/view-renders/wave3-step18-preferences-avalonia-dark.png`
- `artifacts/view-renders/wave3-step18-preferences-avalonia-light.png`
- `artifacts/view-renders/wave3-step18-shell-preferences-dark.png`
- `artifacts/view-renders/wave3-step18-shell-preferences-light.png`
- `artifacts/view-renders/wave3-step18-preferences-invalid.png`
- `artifacts/view-renders/wave3-step18-preferences-periodic-off.png`
- `artifacts/view-renders/wave3-step18-preferences-avalonia-native.png`
- `artifacts/view-renders/wave3-step18-value-dialog.png`
- `artifacts/view-renders/wave3-step18-value-dialog-invalid.png`

The WPF designer-host capture is useful for the page hierarchy but renders
floating hints over their values, unlike the running WPF control. Text-field
internals were therefore compared against the legacy XAML's
`MaterialDesignFloatingHintTextBox` plus a native WPF reference crop, rather
than treating that renderer artifact as the target. The Avalonia isolated and
native-window captures agree on the corrected caption/value/underline
spacing, field widths, path icons, checkbox order, interval placement, type
scale, and palette. The light isolated and full-shell renders prove that text,
fields, navigation, migrated content, dividers, and selection feedback all
switch together. The legacy reference also contains the step-20-only controls
between Editor Reader and the theme switch; their absence and the resulting
earlier theme-switch position are the planned pass boundary, not a redesign
of migrated controls.

The invalid-state capture verifies visible corrections without replacing the
last valid shared settings. The periodic-off capture verifies unchecked
feedback and removal of the dependent interval field without leaving a layout
hole.

The native-window capture was taken from the Release application after
selecting Preferences through UI Automation. Its field density, underlines,
widths, and placement match the Avalonia headless capture; the renderer is
therefore representative for this static layout. Native dialogs remain
outside deterministic image rendering.

Preferences uses the already tested Avalonia storage-provider adapter;
focused view-model tests cover selected and cancelled results without opening
a user-profile-dependent dialog.

## Documentation consulted

Avalonia 12.1 and exact dependency guidance used by this step:

- <https://docs.avaloniaui.net/controls/input/text-input/textbox>
- <https://docs.avaloniaui.net/controls/input/selectors/checkbox>
- <https://docs.avaloniaui.net/controls/input/buttons/togglebutton/>
- <https://docs.avaloniaui.net/api/avalonia/controls/toggleswitch>
- <https://docs.avaloniaui.net/docs/services/file-dialogs>
- <https://docs.avaloniaui.net/docs/data-binding/compiled-bindings>
- <https://docs.avaloniaui.net/docs/styling/themes>
- <https://docs.avaloniaui.net/docs/how-to/styling-controls-how-to>
- <https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>
- <https://github.com/AvaloniaCommunity/Material.Avalonia>
- <https://www.nuget.org/packages/Avalonia/12.1.0>
- <https://www.nuget.org/packages/Material.Avalonia/3.17.0>

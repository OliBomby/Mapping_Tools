# Avalonia UI migration state audit

Date: 2026-08-13

## Intended migration contract

The legacy WPF implementation is the normative UI and behavior specification. A migrated view should keep the same XAML structure, copy, bindings, spacing, tooltips, and interaction flow, except for these deliberate transformations:

1. Replace WPF-only controls and properties with their closest Avalonia equivalents.
2. Move non-visual code-behind behavior into the view model or an injected service.
3. Modernize view models with CommunityToolkit.Mvvm source-generation attributes.
4. Replace tool headers, run buttons, and progress bars with the shared controls while preserving the legacy layout and behavior.
5. Keep a custom control's styles with that control.
6. Compose truly global resource dictionaries from `App.axaml`.

An Avalonia view that merely looks similar in one screenshot is not sufficient if it was structurally rewritten, lost legacy behavior, or introduced new behavior.

## Executive assessment

The current migration is visually promising but does not yet follow this contract consistently.

- `MainWindow` and `GetStartedView` have very good static visual parity in the audited desktop render.
- The three migrated tools and Preferences are recognizable and generally use source-generated MVVM properties and commands.
- The feature XAML was nevertheless rewritten rather than translated minimally. Scroll ownership, copy, tooltips, spacing, bindings, and some control choices differ throughout.
- Important WPF behavior is missing: changelog loading, check-for-updates, Donate, About, exit-without-saving, and maximize/restore icon behavior.
- The shared run and progress controls change the tool execution contract instead of only deduplicating the legacy layout.
- `App.axaml` currently owns both global Material overrides and styles for custom controls. This directly conflicts with the requested ownership model.
- The renderer can produce reassuring screenshots from semantically different states and, for tool views, different hosts. It is not currently a reliable parity gate.

The migration should be treated as a good prototype, not a source-and-behavior-faithful port.

## Severity guide

- **High**: changes or omits user-visible behavior, violates a core migration invariant, or can reject previously valid values.
- **Medium**: material source/layout drift, component ownership problem, or a verification gap that can conceal drift.
- **Low**: localized copy, tooltip, or spacing mismatch with limited behavioral impact.

## Cross-cutting violations

### 1. Feature views were structurally rewritten (High)

`AutoFailDetectorView`, `MapCleanerView`, `RhythmGuideView`, and `PreferencesView` each add their own outer `ScrollViewer` (`Mapping_Tools.Desktop/Views/AutoFailDetectorView.axaml:9`, `MapCleanerView.axaml:9`, `RhythmGuideView.axaml:10`, and `PreferencesView.axaml:25`). The WPF shell owns the feature scroller in `Mapping_Tools/MainWindow.xaml:270-275`; the Avalonia shell now contains only a margined `ContentControl` at `Mapping_Tools.Desktop/Views/MainWindow.axaml:461-465`.

This is not a required WPF-to-Avalonia substitution. It duplicates shell behavior in every view and makes future ports decide scrolling independently. Restore shell-owned scrolling unless a legacy view itself owned a specialized inner scroller.

The same views also paraphrase labels and help text, remove tooltips, and change margins and explicit sizing. These are repeated source-parity violations, not isolated Material-package substitutions.

### 2. Shared controls changed behavior and layout (High)

#### `ToolRunButton`

`Mapping_Tools.Desktop/Controls/ToolRunButton.cs:13-76` implements a Run/Cancel toggle with play/stop icons. The WPF tools use an ordinary run button bound to `CanRun`; they do not expose cancellation through that button. The control therefore introduces a feature and changes the execution contract.

Its global style at `Mapping_Tools.Desktop/App.axaml:208-222` also does not reproduce the legacy structure. The WPF views use a 70-wide `Viewbox` with margin 10 around a button and a 36-by-36 icon. The Avalonia style makes the button itself 70-by-70 with padding 17 and a 42 icon.

#### `ToolProgressBar`

`Mapping_Tools.Desktop/Controls/ToolProgressBar.cs:8-55` hides the bar initially and one second after reaching 100. The WPF progress bar occupies its layout slot and the legacy execution helper resets progress to zero after completion (`Mapping_Tools/Classes/ToolHelpers/SingleRunMappingTool.cs:49-61`). This is another changed behavioral contract.

#### `ToolViewHeader`

`Mapping_Tools.Desktop/Controls/ToolViewHeader.axaml:8-51` centers the title, gives the help popover a 420 maximum width, and the quick-run popover a 300 maximum width. The WPF header and inline tool headers use different margins and up to 600 width (`Mapping_Tools/Components/ViewHeaderComponent.xaml:10-26`). The WPF component also hides an empty description in code-behind; the new control does not preserve that contract.

#### `TimelineControl`

The Avalonia control is a new drawing implementation rather than a direct translation of `Mapping_Tools/Components/TimeLine`. The WPF timeline reserves a 50-high canvas inside its requested 100-high host and sizes relative to the host; `Mapping_Tools.Desktop/Controls/TimelineControl.cs:153-267` draws over the full bounds with its own label metrics. It may be an appropriate Avalonia implementation, but its exact geometry must be reconciled with the legacy layout rather than accepted as a new design.

#### `HotkeyEditor`

`Mapping_Tools.Desktop/Controls/HotkeyEditor.cs:77-133` accepts a hard-coded subset of keys. The WPF `HotkeyEditorControl` accepts all WPF keys except modifiers and a few explicitly excluded keys (`Mapping_Tools/Components/HotkeyEditorControl.xaml.cs:24-54`). OEM punctuation, browser/media keys, and other valid inputs can now be silently ignored. This violates both behavior parity and the repository's rule against arbitrary limits.

### 3. Styles are owned by the wrong layer (High)

`Mapping_Tools.Desktop/App.axaml` contains custom-control styles for:

- `ToolHelpFlyout` presenter behavior (`:28-33`)
- `TimelineControl` brushes (`:93-106`)
- `HotkeyEditor` (`:154-181`)
- tool information badges (`:199-207`)
- `ToolRunButton` (`:208-222`)
- `ToolProgressBar` (`:223-226`)

These belong with the components. `App.axaml` also contains broad overrides for TextBox, Menu, ComboBox, path-picker buttons, and compact combo boxes. Meanwhile additional styles exist in `MainWindow.axaml`, `GetStartedView.axaml`, `PreferencesView.axaml`, and both dialog windows. `Resources/MappingToolsColors.axaml` is the only separate global dictionary.

Recommended ownership model:

- `App.axaml`: theme declarations and merged dictionary references only.
- `Resources/Styles/TextBoxes.axaml`, `ComboBoxes.axaml`, `Menus.axaml`, and `CommonButtons.axaml`: application-wide Material compatibility overrides, merged by `App.axaml`.
- `Controls/<Control>.axaml` or a control-owned `Themes/Generic.axaml`: templates and styles for code-only custom controls, merged once for Avalonia discovery but maintained beside the component.
- A view's resources: genuinely view-specific styles, such as the `GetStartedView` replacement for WPF `ListView`/`GridView`.
- `MainWindow.axaml`: shell-only styling.

Prefer explicit classes for legacy-parity adaptations when the style is not truly universal. Broad default selectors such as the current TextBox and ComboBox overrides silently change every future view.

### 4. View-model modernization is mostly good, with two notable violations (Medium)

Most migrated view models use `[ObservableProperty]` and `[RelayCommand]` effectively. The major exceptions are in `RhythmGuideViewModel`:

- `SourcePathsText` (`Mapping_Tools.Desktop/ViewModels/RhythmGuideViewModel.cs:40-42`) replaces a typed path collection with a UI-formatted string that is repeatedly parsed. The migration control-parity guidance explicitly rejects parallel `FooText` state for typed values.
- `OnSourcePathsTextChanged` manually raises `SourceCount` at `:398-400`; if this state remains, `[NotifyPropertyChangedFor(nameof(SourceCount))]` is the source-generated form.

The manual adapter properties in `PreferencesViewModel` are justified because they update shared settings and trigger side effects. Dynamic validation notification in `ValueDialogViewModel` is also justified.

### 5. Tests encode the new implementation, not WPF parity (Medium)

The platform tests cover view models and control mechanics, but there is no AXAML structure/parity check and no behavior inventory test ensuring every WPF command and branch still exists. `ToolControlTests` validates the new progress hide/show behavior, which makes the changed contract look intentional without comparing it to WPF.

Add parity-focused tests for shared-control contracts, command availability, typed value semantics, and every migrated view's behavior inventory. Do not attempt brittle full-text XAML equality; instead enforce the important invariants and review a minimal WPF/Avalonia diff.

## Per-view findings

### `AutoFailDetectorView`

Legacy source: `Mapping_Tools/Views/AutoFailDetector/AutoFailDetectorView.xaml` and `.xaml.cs`.

Avalonia source: `Mapping_Tools.Desktop/Views/AutoFailDetectorView.axaml` and `Mapping_Tools.Desktop/ViewModels/AutoFailDetectorViewModel.cs`.

- **High:** Adds a view-owned outer scroller instead of retaining shell-owned scrolling.
- **Medium:** `Auto-insert spinners` is disabled unless `Get auto-fail fix` is checked (`AutoFailDetectorView.axaml:33`). The WPF checkbox remains independently enabled.
- **Low:** Header text is paraphrased rather than copied.
- **Low:** Detailed tooltips were shortened or removed, including the auto-fix options, HardRock context, 120 fps analysis, and lag explanation.
- **Low:** Legacy checkbox `FontSize="14"` and hand cursors are lost.
- **Medium:** Progress staging changed from 33/67/100 in WPF code-behind to 10/100 in `AutoFailDetectorViewModel.cs:172-180`.
- **Medium:** WPF offers the fix dialog after analysis whenever fix mode is selected; Avalonia only offers it when `PotentialUnloadingObjects.Count > 0` (`:190-194`).
- **High:** The shared run button adds cancellation, which WPF did not expose.
- **Pass:** Analysis/fix orchestration was moved out of view code-behind, and the VM uses source-generation attributes.

### `MapCleanerView`

Legacy source: `Mapping_Tools/Views/MapCleaner/CleanerView.xaml` and `.xaml.cs`.

Avalonia source: `Mapping_Tools.Desktop/Views/MapCleanerView.axaml` and `Mapping_Tools.Desktop/ViewModels/MapCleanerViewModel.cs`.

- **High:** Adds a view-owned outer scroller.
- **Low:** Header text is paraphrased.
- **Low:** Detailed tooltips for the cleaner options and the Signatures section were removed; only a shortened beat-divisors tooltip remains.
- **Medium:** A multi-map run sets `HasRun = true` but supplies no markers (`MapCleanerViewModel.cs:265-270`), producing an empty visible timeline. WPF never successfully adds a timeline for its multi-map path.
- **Low:** Autosave restoration moved from synchronous construction to asynchronous activation, allowing defaults to appear briefly before restoration.
- **High:** The shared run/progress behavior differs from WPF as described above.
- **Pass:** Cleaner execution and autosave behavior are now largely represented in a source-generated VM rather than view code-behind.

### `RhythmGuideView`

Legacy source: `Mapping_Tools/Views/RhythmGuide/RhythmGuideView.xaml` and `.xaml.cs`.

Avalonia source: `Mapping_Tools.Desktop/Views/RhythmGuideView.axaml` and `Mapping_Tools.Desktop/ViewModels/RhythmGuideViewModel.cs`.

- **High:** Adds a view-owned outer scroller.
- **High:** Replaces typed source paths and the WPF converter with `SourcePathsText`, introducing duplicate string state and manual parsing.
- **Low:** The map count renders as `12 maps total`; WPF renders `(12) maps total`.
- **Low:** Tooltips for export path, combo boxes, output name, and selection mode were removed or rewritten.
- **Medium:** Export Browse changes meaning by mode. WPF always uses a beatmap open picker; Avalonia uses a save picker for New Map (`RhythmGuideViewModel.cs:206-239`). This may be a useful improvement, but it is unapproved migration drift.
- **Medium:** WPF backs up every source path before generation. The new service backs up before writing the destination, changing when and which files are backed up.
- **Medium:** Completion behavior changed. WPF returns an empty message for New Map and `Done!` otherwise; Avalonia always reports an object-count summary and reveals a new file (`:263-284`).
- **High:** The shared run button adds cancellation.
- **Pass:** Most non-visual generation behavior has been moved into the VM/service boundary.

### `RhythmGuideWindow`

Legacy source: `Mapping_Tools/Views/RhythmGuide/RhythmGuideWindow.xaml` and `.xaml.cs`.

Avalonia source: `Mapping_Tools.Desktop/Views/RhythmGuideWindow.axaml` and `.axaml.cs`.

- **High:** This is a redesign, not a constrained port. WPF uses borderless custom chrome, a 35-pixel close row, a one-pixel border, ten-pixel content margin, drag-to-move, double-click maximize/restore, and resize behavior. Avalonia uses native chrome, owner centering, `MinWidth="520"`, `MinHeight="400"`, and a 20-pixel padded border.
- If native chrome is preferred for cross-platform behavior, record it as an explicit approved exception and preserve the remaining dimensions/content layout as closely as possible.

### `PreferencesView`

Legacy source: `Mapping_Tools/Views/Preferences/PreferencesView.xaml`.

Avalonia source: `Mapping_Tools.Desktop/Views/PreferencesView.axaml` and `Mapping_Tools.Desktop/ViewModels/PreferencesViewModel.cs`.

- **High:** Adds an outer scroller and fixed 900-pixel inner width rather than retaining the legacy view/shell sizing model.
- **Medium:** Density changed throughout: title weight Bold to Medium, row margins 10 to 8, `MaxWidth="150"` fields to fixed `Width="150"`, and an added 30-pixel checkbox minimum height.
- **Low:** Detailed settings tooltips are repeatedly shortened.
- **Medium:** The two WPF `ToggleButton`s were replaced by `ToggleSwitch` even though Avalonia has a ToggleButton. This is a visual and interaction change not required by platform differences.
- **High:** `[Range(1, 100_000)]` on maximum backup files (`PreferencesViewModel.cs:63-69`) rejects values WPF accepted. Zero is meaningful to the backup logic, and the project explicitly forbids arbitrary hard limits.
- **High:** `[MinimumTimeSpan("00:00:01")]` (`:72-77`) adds another limit absent from WPF.
- **High:** `HotkeyEditor` accepts fewer keys than the legacy editor.
- **Pass:** Most view behavior has been moved into a source-generated VM; the adapter properties appropriately preserve live settings side effects.

### `GetStartedView` / legacy `StandardView`

Legacy source: `Mapping_Tools/Views/Standard/StandardView.xaml` and `.xaml.cs`.

Avalonia source: `Mapping_Tools.Desktop/Views/GetStartedView.axaml` and `Mapping_Tools.Desktop/ViewModels/GetStartedViewModel.cs`.

- **High:** The WPF view fetches GitHub releases and populates its changelog (`StandardView.xaml.cs:31-53`). Avalonia initializes `Changelog` as an empty collection and never fills it (`GetStartedViewModel.cs:32-48`).
- **Medium:** `HasNoRecentMaps` exists but is not bound, so the promised empty-state behavior is absent.
- **Medium:** Static onboarding copy was moved from XAML into the VM. It is presentation content, not behavior, and this increases source divergence.
- **Medium:** `TableView` is a reasonable replacement for WPF `ListView`/`GridView`, but the 143-line local template and hard-coded measurements are a brittle visual reimplementation. The first/second column sizing no longer follows the legacy auto-content behavior.
- **Low:** The view introduces local font size/weight defaults that differ from the legacy inherited shell typography.
- **Pass:** In the full-shell 1280-by-800 render, the static layout is very close to WPF.

### `MainWindow`

Legacy source: `Mapping_Tools/MainWindow.xaml` and `.xaml.cs`.

Avalonia source: `Mapping_Tools.Desktop/Views/MainWindow.axaml`, `.axaml.cs`, and `Mapping_Tools.Desktop/ViewModels/MainViewModel.cs`.

- **High:** Check for updates, Donate, and About are present but disabled (`MainWindow.axaml:244-269`); all work in WPF.
- **Medium:** The close-button context menu action `Exit without saving` was omitted.
- **Medium:** About-menu tooltips and current-map menu details/icons were removed or changed.
- **Medium:** The maximize button never switches to a restore icon or adjusts chrome layout when maximized. WPF handles those state changes in `MainWindow.xaml.cs:455-495`.
- **High:** The shell-owned content scroller was removed, causing every ported feature to reinvent it.
- **Medium:** WPF `DialogHost`/Snackbar behavior was replaced with external windows and a custom notification surface. Material.Avalonia differences may justify this, but it is a major behavior/layout exception and is not documented as such.
- **Medium:** Production feature registration order is Rhythm Guide, Auto-Fail Detector, Map Cleaner (`Mapping_Tools.Desktop/Composition/DesktopFeatureRegistrationExtensions.cs:26-47`), while WPF sorts tools alphabetically. The renderer hides this by injecting a synthetic alphabetic registry.
- **Pass:** The static 1280-by-800 full-shell render is exceptionally close to WPF.
- **Pass:** Remaining code-behind is mostly visual window and pointer behavior, which is appropriate to keep in the view.

### `MessageDialogWindow`

Closest legacy source: `Mapping_Tools/Components/Dialogs/MessageDialog.xaml`. The separate legacy `Views/Standard/MessageWindow` is not equivalently represented.

- **High:** The embedded WPF `DialogHost` user control became a native owner-modal window. That changes chrome, focus, stacking, and layout and must be an explicit compatibility exception.
- **Medium:** The new dialog generalizes the fixed legacy choice layout and details presentation instead of preserving the legacy source.
- **Medium:** Its action-button style duplicates `ValueDialogWindow`'s local style.
- **Medium:** If this window is also intended to replace legacy `MessageWindow`, it lacks that window's collapsible error details and custom borderless chrome.

### `ValueDialogWindow`

Legacy source: `Mapping_Tools/Components/Dialogs/TypeValueDialog.xaml`.

- **High:** Like MessageDialog, the embedded `DialogHost` component became a native owner-modal window without a recorded exception.
- **Medium:** The dialog action style is duplicated rather than shared.
- **Medium:** `ValueDialogWindow.axaml.cs:26-42` constructs the validation binding programmatically. Focus/select-all behavior belongs in the view, but value/validation behavior should be exposed by the VM or a reusable control so the XAML remains declarative.

## `AGENTS.md` audit

The root `AGENTS.md` clearly communicates product values, XML-documentation requirements, and the preference for simple systems. It does not state the migration contract, so an agent can plausibly produce a visually similar redesign while believing the task is complete.

Add a dedicated Avalonia migration section with these enforceable rules:

1. WPF XAML, code-behind, and VM behavior are the normative specification.
2. Keep the AXAML structurally identical; only enumerated platform translations and approved shared-control substitutions are allowed.
3. Do not paraphrase copy, remove tooltips, alter spacing, change validation ranges, add commands, or redesign interactions during migration.
4. Inventory every WPF event handler, command, binding, validation rule, converter, tooltip, context-menu item, dialog, and completion branch before coding.
5. Preserve shell-owned scrolling; only migrate inner scrolling that exists in the legacy view.
6. Use `ToolViewHeader`, `ToolRunButton`, and `ToolProgressBar`, but require those controls to reproduce the WPF layout and contract.
7. Keep custom-control styles with their controls. Keep application-wide Material compatibility styles in focused dictionaries composed by `App.axaml`.
8. A deliberately different platform behavior must be listed in a per-view exception record and approved; it cannot be silently treated as parity.
9. Missing or deferred legacy behavior means a view is not complete.
10. Require same-state WPF/Avalonia render evidence plus focused behavioral tests before declaring a view migrated.

The current code also contradicts the existing “no arbitrary hard limits” rule through the Preferences ranges and HotkeyEditor whitelist.

## Skills audit

### `migrate-ui`

Strong points:

- Requires a behavior inventory before migration.
- Correctly favors source-generated MVVM state and commands.
- Calls for exact layout and compiled bindings.
- Its control-parity reference correctly warns against string shadow properties such as `FooText`.

Violations/gaps in the skill itself:

- “Copy the exact layout” is weaker than a minimal structural source-diff rule. It allowed visually similar rewrites.
- It does not mandate the shared header/run/progress controls or define their legacy contracts.
- It does not state shell-owned scrolling as an invariant.
- It says to use application-level styles registered in `App.axaml` and only “prefer” reusable styles. This encouraged the present `App.axaml` concentration and conflicts with component-owned styles.
- It lacks a style-ownership taxonomy distinguishing component, view, shell, and application-wide Material compatibility styles.
- It permits “intentionally deferred behavior” in a final report without making that status incomplete. This lets disabled menu actions and the missing changelog pass a migration handoff.
- Its completion criteria do not require a method-by-method comparison against legacy code-behind.
- It does not forbid opportunistic improvements such as cancellation, new picker semantics, or new validation limits during a port.

`references/control-parity.md` is otherwise valuable, but its advice to put non-outlined TextBox styles directly in `App.axaml` should instead point to a focused resource dictionary merged by the app.

### `migrate-feature`

The instruction to copy code exactly where possible and the requirement to use `migrate-ui` are directionally correct. It should additionally say that migration is not a product-improvement pass, require an explicit WPF behavior checklist, and state that unapproved UI or behavior differences block completion.

### `render-desktop-view`

The written skill correctly requires the same size, theme, font, and semantic state. The renderer implementation currently violates that requirement:

- The WPF ordinary-view host adds 20 pixels of padding (`tools/Mapping_Tools.Wpf.ViewRenderer/Program.cs:123-131`), while the Avalonia AutoFail, MapCleaner, and RhythmGuide factories return raw views (`tools/Mapping_Tools.Avalonia.ViewRenderer/Program.cs:278-333`). The image pairs are not using the same host.
- RhythmGuide uses 12 synthetic source paths only on Avalonia. Preferences fixtures also differ in path/hotkey values. These are not same-state comparisons.
- The Avalonia MainWindow renderer injects a synthetic full, alphabetically arranged tool registry (`Program.cs:167-215`) and therefore conceals the production tool-order difference.
- The WPF StandardView's asynchronous changelog load is not awaited, allowing both screenshots to appear empty and hiding the missing Avalonia feature.
- Rendering MapCleaner can attempt to load real temporary autosave state, making the scenario less deterministic than the skill promises.

Until these fixtures are symmetric, renders are useful visual clues but not acceptance evidence.

### `write-unit-tests`

The test naming, Arrange/Act/Assert, and Fluent Assertions rules are sound and do not conflict with the migration vision. Consider adding a pointer from `migrate-ui` to a parity-testing checklist rather than expanding this general unit-test style skill.

## Recommended remediation order

1. Amend `AGENTS.md` and `migrate-ui` first. Otherwise future agents will continue creating the same class of drift.
2. Define and test the exact legacy contracts of `ToolViewHeader`, `ToolRunButton`, `ToolProgressBar`, `TimelineControl`, and `HotkeyEditor`; then move their styles to component-owned resources.
3. Split Material compatibility styles into focused dictionaries and reduce `App.axaml` to composition.
4. Restore the shell scroller and re-port each feature AXAML as a minimal WPF translation, preserving copy, tooltips, bindings, order, and measurements.
5. Restore missing MainWindow and GetStarted behaviors before adding more views.
6. Remove the new Preferences limits and fix HotkeyEditor input parity.
7. Decide and document the two legitimate architectural exceptions: native dialog/windows versus WPF `DialogHost`/custom chrome, and the Avalonia replacement for WPF `ListView`/`GridView`.
8. Make render fixtures symmetric and add behavior-parity coverage.

## Verification performed

The following paired 1280-by-800 renders were generated and visually inspected for Avalonia and WPF: MainWindow, Get Started, Preferences, Rhythm Guide, Auto-Fail Detector, and Map Cleaner. MainWindow and Get Started show strong static parity; the other pairs expose fixture/host asymmetry and cannot serve as exact comparison evidence.

The already-built platform test assembly passes all 188 tests. A clean recompilation could not be verified because a renderer-owned `.NET Host` process held the desktop output DLLs open. That lock is an infrastructure verification limitation, not a product test failure. No production source was changed during this audit.

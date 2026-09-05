# Solution quality and legacy UI audit

Date: 2026-09-05. Reference: `.reference/Mapping_Tools-Original/Mapping_Tools`.

The solution has a coherent Core/Application/Infrastructure/Desktop dependency structure and corresponding views for all 19 legacy tools. It is **not yet functionally equivalent in every UI interaction**. The most urgent findings are unawaited shutdown autosaves and execution with invalid field text. Passing unit tests do not establish visual equivalence.

This was a solution-wide static audit of project structure, tool/view inventory, UI labels and options, dependencies, validation, execution, persistence and platform wiring, with deeper inspection of the shared infrastructure and selected feature paths. It was not an exhaustive line-by-line proof of every algorithm. No production code was changed.

## Prioritized findings

### 1. P1 — Await project autosaves before allowing shutdown to finish

Location: `Mapping_Tools.Desktop/Services/ProjectAutosaveCoordinator.cs:54–59`.

`SaveOnShutdown` discards `SaveAutosaveAfterLoadAsync`'s task. `MainViewModel.Dispose` invokes it for instantiated features (`MainViewModel.cs:172–173`), but neither it nor `App.StopHost` waits for these saves. The production project store really performs asynchronous writes, so the method can return before the temporary document is flushed and moved into place. Pending project loads introduce another asynchronous boundary before saving even starts.

Closing the app can therefore leave an earlier autosave instead of the current project, especially for large projects or slow storage. Atomic replacement protects the previous file from truncation; it does not ensure the new session is saved.

Recommendation: expose an awaitable shutdown-save operation and await all project saves before ending the desktop lifetime/disposing the host. Keep the UI dispatcher available until any outstanding load/snapshot work finishes. Test shutdown with a deliberately delayed project store.

Evidence: confirmed by the lifecycle and asynchronous store call paths; an actual interrupted-write shutdown was not performed.

### 2. P1 — Block runs when text bindings contain conversion errors

Location: `Mapping_Tools.Desktop/ViewModels/SingleRunToolViewModel.cs:73–76,125–128`.

`PrepareRun` checks only the view model's data-annotation errors. Invalid text converted by `InvariantDoubleConverter` produces an Avalonia `DataValidationError`, leaving the underlying numeric property at its previous valid value. That control-level error never becomes an `ObservableValidator.HasErrors` entry. Clearing focus does not fix this mismatch.

An isolated runtime probe using a real Avalonia TextBox, the production converter and the shared run pipeline produced:

```text
Invalid text: control errors=True, VM errors=False, retained number=256
Invalid text: tool executed=True
```

Thus a form can visibly contain invalid input while executing with stale settings. Slider Merger's leniency is one concrete binding that uses this combination. Other converter-backed forms need the same check. This is a present correctness issue; the audit does not assert that every legacy tool handled it correctly.

Recommendation: connect binding validation to the run gate, or validate editable text through presentation state before producing typed execution options. Test the complete edit-invalid-text → Run interaction, not only converter return values or direct property assignment.

### 3. P2 — Register an execution before it can finish

Location: `Mapping_Tools.Application/Execution/ToolExecution/ToolExecutionService.cs:77–84`.

`RunAsync` starts before the operation is inserted in `running`. With an already-cancelled token, its awaited `Task.Run` is already cancelled, and the catch/finally can finish synchronously. The finally removes a key that has not yet been added; `ExecuteAsync` then adds the completed operation permanently.

Runtime reproduction:

```text
Cancelled run: Cancelled; IsRunning: True; Retry: AlreadyRunning
```

Every later invocation with that operation ID is rejected until the coordinator is replaced. Completion must not be able to remove the registration before it exists. Add a regression test for an already-cancelled token and a subsequent successful retry.

### 4. P2 — Remove the selected sequence occurrence, not the first matching colour

Locations: `Mapping_Tools.Desktop/Tools/ComboColourStudio/Views/ComboColourStudioView.axaml:102–103`; `ViewModels/ComboColourStudioViewModel.cs:217–224` in the same tool.

Sequence entries reuse the palette's `ObservableSpecialColour` instances. The row's remove button passes that shared colour object, and the command calls `ColourSequence.Remove(colour)`. For the sequence A, B, A, clicking the third row's remove button removes the first A.

Runtime reproduction returned **B, A**, where the selected-row operation should produce **A, B**. The resulting combo-colour order changes incorrectly.

Recommendation: pass the sequence index or a distinct entry object identifying that occurrence. Cover repeated palette references in the regression test.

### 5. P2 — Preserve legacy deletion of multiple selected colour points

Locations: `Mapping_Tools.Desktop/Tools/ComboColourStudio/Views/ComboColourStudioView.axaml:20–24`; `ViewModels/ComboColourStudioViewModel.cs:173–177`.

The grid permits `SelectionMode="Extended"`, but only binds `SelectedItem`. The remove command deletes just `SelectedColourPoint`; no selected-items collection is passed or synchronized.

The legacy `Classes/Tools/ComboColourStudio/ComboColourProject.cs:43–45` removes all points whose `IsSelected` is true. Selecting several points and pressing minus therefore has different behavior in the port.

Recommendation: expose the actual selected collection to the command and retain the legacy last-point fallback when nothing is selected. Verify a three-point selection and removal in the view.

Evidence: confirmed static difference in the selection and command wiring; no native grid input automation was performed.

### 6. P2 — Restore persistent labels in Hitsound Studio dialogs

Location: `Mapping_Tools.Desktop/Tools/HitsoundStudio/Views/HitsoundStudioExportDialog.axaml:61–80`, with the same pattern in its import dialog.

The export dialog presents two sample-format ComboBoxes with identical item sets and no visible field labels. Their purposes are distinguishable only by tooltips. Other fields use placeholders that disappear once populated. The legacy export dialog explicitly labels “Sample file format”, “Mixed sample file format”, export mode, game mode and grouping leniency with floating hints (`HitsoundStudioExportDialog.xaml:52–108`).

This is a concrete UI-equivalence and discoverability regression: after entering values, users cannot identify some settings at a glance. Restore visible labels using the shared Material field styles. The controls and underlying options are present; this is not missing export functionality.

Evidence: confirmed AXAML/XAML difference, not a screenshot comparison.

## Requirements gaps, separate from port regressions

### Cross-platform live integration is incomplete

`DesktopServiceRegistration.cs:153–166` registers unsupported live-reader/current-map/reload adapters outside Windows. `UnsupportedPlatformCurrentBeatmapLocator.cs:16` always returns null. Global hotkeys and BetterSave override similarly use unsupported adapters. Geometry Dashboard registers Windows adapters (`GeometryDashboardToolDefinition.cs:49–55`); those adapters explicitly report no support on other platforms.

The application can have a cross-platform frontend and file-based tools while still lacking the stated cross-platform interaction requirement. Selected-object and current-editor workflows cannot be considered equivalent on Linux/macOS. Track this explicitly as a product requirement gap and provide clear capability feedback in the UI until implemented. osu!lazer and Wine live behavior were not exercised and should not be certified from these tests.

### Mapset Merger retains an arbitrary 200-file cap

`Mapping_Tools.Application/Tools/MapsetMerger/MapsetMergerService.cs:375–378` rejects source sets with more than 200 beatmaps or storyboards. The same cap exists in legacy `Views/MapsetMerger/MapsetMergerView.cs:30,317,332`, so this preserves legacy behavior but conflicts with the current instruction against arbitrary hard limits.

Remove the cap in favor of cancellation/progress, or document an actual format constraint if one exists. It is not a newly introduced parity regression.

## UI coverage

“No specific gap found” means the static label/option comparison did not identify an actionable discrepancy for that screen. It does **not** certify rendered layout, every event path or transformation output.

| Legacy tool/surface | Audit result |
| --- | --- |
| Auto Fail Detector | Corresponding view; no specific label/option gap found. |
| Combo Colour Studio | Confirmed sequence-removal and multiple-selection gaps; sequence editing moved from a grid editing template to a separate pane. |
| Snapping Tools / Geometry Dashboard | Corresponding main/preferences/project/generator-settings views; live platform support incomplete. |
| Hitsound Copier | Corresponding controls; renamed mode labels are supplied by a converter. |
| Hitsound Preview Helper | Corresponding view; no specific label/option gap found. |
| Hitsound Studio | Main/import/export surfaces present; persistent labels regressed in dialogs. |
| Map Cleaner | Corresponding view; no specific label/option gap found. |
| Mapset Merger | Corresponding view; inherited 200-file cap conflicts with current requirements. |
| Metadata Manager | Corresponding view; colour-picker/HEX presentation differs and needs rendered comparison. |
| Pattern Gallery | Corresponding cards/options; context menus are created in code, so absent static menu strings are not missing functionality. |
| Property Transformer | Corresponding view; shared binding-validation concern applies. |
| Rhythm Guide | Main and secondary window present; no specific main-screen label/option gap found. |
| Sliderator | Corresponding view; no specific label/option gap found. |
| Slider Completionator | Corresponding view; no specific label/option gap found. |
| Slider Merger | Corresponding view; numeric binding exhibits the shared validation mismatch. |
| Slider Picturator | Corresponding view; no specific label/option gap found. |
| Timing Copier | Corresponding options; legacy resnap descriptions supplied through a converter. |
| Timing Helper | Corresponding view; no specific label/option gap found. |
| Tumour Generator | Corresponding view and value/graph control; no specific main-screen label/option gap found. |
| Main window and Preferences | Static label/menu inventory matches; shutdown persistence has the finding above. |
| Shared graph, timeline, dialogs, progress and run controls | Source and existing test coverage inspected selectively; invalid-text-to-run integration is missing. |

## Code-quality assessment

- Project dependency directions are coherent, and all five architecture tests pass. Core contains calculations and models, Application exposes use cases/ports, Infrastructure owns external adapters, and Desktop owns presentation. The sample plugin builds with the solution.
- Filesystem-backed project writes use temporary sibling files and atomic replacement. Mapset Merger stages output and attempts rollback. These are useful protections, but they do not address the shutdown-await issue.
- The isolated test run emitted 15 distinct compiler-warning lines, all nullability-related, across test code and Infrastructure. Earlier compilation also emitted Core nullability warnings. A warning-free baseline has not been established; do not hide these globally with suppressions.
- Six `using` aliases remain in production source, contrary to the explicit repository rule, including aliases in `WindowsEditorReaderAdapter.cs`, `WindowsGlobalHotkeyService.cs`, `LegacyProjectJsonSerializer.cs` and Geometry Dashboard Core files.
- Some public XML documentation is present but incomplete: e.g. `Core/HitsoundStuff/CustomIndex.cs` has empty parameter/return descriptions. Finish useful API contracts as those files are touched rather than adding empty documentation to satisfy a check.
- Some files remain large, notably the legacy project serializer, graph control and Hitsound Studio view model. Size alone is not a defect; avoid a broad abstraction rewrite. Prioritize the demonstrated lifecycle, identity and validation problems.
- The legacy JSON reader still delegates unknown type names to `DefaultSerializationBinder` with object type metadata enabled. Its own documentation limits it to trusted files. The ordinary project-loading path reaches this reader for unversioned JSON; an allowlist review is warranted before claiming safe import of arbitrary shared legacy projects. No exploitation claim is made by this audit.

## Verification and limits

The initial default-output test command hit DLL locks from the running desktop app. The authorized isolated-output fallback succeeded:

```text
dotnet test Mapping_Tools.slnx --no-restore -p:OutputPath=bin/agent/ --verbosity quiet
Architecture:       5 passed
Core:             173 passed
Application:      163 passed
Infrastructure:   141 passed
Desktop:          255 passed
Total:            737 passed, 0 failed, 0 skipped
```

Three additional isolated runtime probes reproduced findings 2–4. They used already-built assemblies after a project-reference probe restore encountered restricted NuGet network access. No new dependencies were installed. Supporting scratch artifacts are under ignored `bin/agent/`: `audit-probe/`, `audit-probe-results.txt`, `audit-tests.log`, and `audit-ui.ps1`.

Native side-by-side screenshots, DPI/theme/resizing behavior, keyboard/focus traversal, actual osu! live integration, audible output and Linux/macOS execution were not verified. Pixel-level equivalence remains an explicit open verification step. The static comparisons are not a substitute for it.

Recommended fix order: await shutdown autosaves; bridge control validation into execution; fix execution registration ordering; correct Combo Colour Studio occurrence/selection handling; restore dialog labels; then run a native visual and interaction pass against the legacy app.

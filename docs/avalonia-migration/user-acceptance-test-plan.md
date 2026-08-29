# Mapping Tools Avalonia migration user acceptance test plan

Status: final migration UAT baseline, updated 2026-08-18
Applies to: migration from the legacy WPF/WinForms application to `Mapping_Tools.Desktop` on Avalonia 12.1.0  
Related plan: [feature-dependency-graph.md](feature-dependency-graph.md)

## 1. Purpose

This plan defines how users will decide whether each migrated feature, each migration wave, and the final Avalonia application are acceptable replacements for the legacy application.

Acceptance is based on observable behavior, data safety, saved-data compatibility, workflow usability, and platform integration. Compilation or visual resemblance alone is not acceptance.

## 2. Objectives

UAT must demonstrate that:

1. A mapper can complete the same supported workflows in Avalonia as in the legacy application.
2. Given equivalent inputs and options, transformation tools produce semantically equivalent beatmaps, storyboards, projects, collections, and exported files.
3. Destructive operations preserve the established backup, cancellation, error, and editor-reload protections.
4. Existing settings and feature project files load without silent loss or have an explicitly tested migration.
5. Navigation, validation, dialogs, progress, notifications, keyboard workflows, and accessibility are usable.
6. Windows-only integrations are isolated, clearly reported, and do not prevent portable features from running.
7. The Avalonia build can replace the default executable and be rolled back safely.

## 3. Scope

### Included

- Application shell, Get started, navigation, search, favorites, notifications, window state, theme, and shutdown behavior.
- Settings, file/folder selection, current-map selection, recent maps, drag/drop, backups, QuickUndo, project lifecycle, and QuickRun.
- Every user-facing tool listed in the feature dependency graph.
- Auxiliary windows, typed dialogs, common form controls, timeline, object visualizer, graph editor, overlay, updater, and packaging workflows.
- Compatibility with existing user settings, feature project JSON, pattern collections, `.osu`/`.osb` files, hitsound schemas, and export layouts.
- Supported failure, validation, cancellation, cleanup, and recovery paths.
- Static visual parity at representative sizes and interactive usability in a real desktop session.

### Excluded unless separately scheduled

- New functionality that did not exist in the legacy application.
- Deliberate redesigns approved before test execution.
- Unsupported operating systems or osu! clients.
- Exact pixel equality where WPF and Avalonia differ only in text rasterization or other non-functional platform rendering details.
- Algorithm performance benchmarking beyond detecting a material user-visible regression.

## 4. Acceptance model

UAT is performed at three levels.

| Gate | Scope | Approval required |
|---|---|---|
| Feature gate | One migrated view or bounded feature slice | Feature owner and at least one mapper familiar with the legacy feature |
| Wave gate | Integrated workflows and shared dependencies introduced by a migration wave | Migration owner and representatives for affected features |
| Release gate | Complete Avalonia application, packaging, update, rollback, and retained legacy user-data compatibility | Product/release owner and designated user acceptance group |

Historical feature-gate evidence may compare the Avalonia frontend with the legacy implementation. The legacy frontend was retained through step 48 and is removed only by the explicitly approved step 49 cutover.

## 5. Roles and responsibilities

| Role | Responsibilities |
|---|---|
| Migration owner | Prepares builds and fixtures, records known differences, triages defects, and verifies entry criteria. |
| Feature owner | Defines representative workflows and expected results, including edge cases learned from real use. |
| Acceptance tester | Executes tests as a user, records evidence, and decides whether behavior is usable and correct. |
| Data-safety reviewer | Reviews transformations, backups, overwrite prompts, recovery, and compatibility results. |
| Platform-integration tester | Tests osu! integration, global input, audio, filesystem, overlays, DPI, and multi-monitor behavior. |
| Release owner | Approves cutover, rollback readiness, supported-platform statements, and remaining known issues. |

Agents may prepare fixtures, render views, execute repeatable scripts, compare structured output, and collect logs. Human testers retain responsibility for subjective usability, audio perception, real osu! interaction, global hotkey interference, cursor/overlay alignment, and final acceptance.

## 6. Test environments

Record the exact values for every test session rather than relying on a shared implicit environment.

| Dimension | Required coverage |
|---|---|
| Operating system | Primary supported Windows version for all gates; each additional claimed OS before release acceptance. |
| Architecture | Every architecture that will be published. |
| Display | 100% scaling; one high-DPI configuration; minimum supported window size; representative wide display. |
| Theme | Dark and light where supported; operating-system default variant if selectable. |
| Input | Mouse and keyboard; touch/pen only if claimed as supported. |
| osu! state | Not running, running outside editor, editor open, map selected, objects selected, and integration unavailable. |
| Storage | Normal writable folders, read-only/denied destination, missing source, long/unicode paths, and low-risk simulated I/O failure. |
| Audio | No device, default device, representative WAV/OGG/MP3 inputs, MIDI/SF2 where applicable. |
| User state | Clean profile, migrated legacy profile, and intentionally malformed/corrupt settings or project data. |
| Network | Online and offline for update/help/release-dependent workflows. |

### Safety setup

- Use copies of beatmaps and disposable Songs/app-data directories.
- Never execute destructive UAT against the tester's only copy of a mapset.
- Preserve the original fixture tree read-only and write results to a per-run directory.
- Disable or isolate real global hotkeys until the manual in-app command is accepted.
- Record the osu! process/version and editor state for integration cases.
- Remove generated overlays, hooks, audio handles, and temporary files at the end of each session.

## 7. Test data and fixture catalog

Wave 0 must establish versioned fixture catalogs owned by the test projects that consume them. Each fixture receives a stable purpose and is covered by a focused test or a fixture-driven characterization test.

Minimum fixture groups:

| Fixture group | Required representatives |
|---|---|
| Beatmaps | Simple standard map, multi-difficulty mapset, mania/other supported modes, inherited timing, sliders of every supported path type, bookmarks, samples, colours, storyboard events, malformed-but-tolerated input, unicode metadata and paths. |
| Transformations | Before/after fixture for every destructive tool, including empty selection, single item, multiple items, boundary values, and no-op options. |
| Projects | One legacy project per `ISavable<T>` feature, default project, populated project, unknown fields, older schema, malformed JSON. |
| Patterns | Beatmap import, code import, loose file, ZIP collection, conflicting names, invalid entry, and placement transformations. |
| Hitsounds/audio | Samples, storyboard samples, custom indices, schema files, WAV/OGG, MIDI, SF2, missing media, unsupported/corrupt media. |
| Mapsets | No conflicts, each conflict category, unicode filenames, storyboards, shared media, missing files, read-only destination. |
| Settings | Clean defaults, representative legacy settings, favorites/hotkeys, invalid paths, skipped update, window bounds, and corrupt JSON. |
| Platform failures | osu! absent, EditorReader unavailable, permission denied, file locked, audio device absent, network offline, cancelled picker. |

For text-based outputs, retain both exact expected files and a semantic comparison report. Semantic comparison must account for intentionally normalized ordering or formatting while still detecting changed map meaning.

## 8. Entry criteria

### Feature UAT entry

- The selected feature is implemented and registered in the Avalonia shell.
- The Avalonia frontend and framework-neutral projects build from the candidate commit.
- Relevant automated unit, characterization, fixture, and architecture tests pass.
- Required shared services from earlier waves are accepted or explicitly included in the current test scope.
- The legacy behavior and known legacy defects used as migration baselines are documented.
- Representative migration renders exist for deterministic states where visual comparison was required.
- Test fixtures, expected outputs, and recovery copies are prepared.
- Deferred behavior and intended design differences are declared before execution.
- No known defect can corrupt or overwrite the tester's uncontrolled data.

### Release UAT entry

- Every feature has passed its feature gate or has an approved deprecation.
- All wave integration gates have passed.
- Installers/packages exist for every claimed platform and architecture.
- Upgrade from legacy installations, update, uninstall, and rollback procedures are documented and testable.
- No unresolved severity 1 or severity 2 defect remains.

## 9. Common acceptance scenarios

Run applicable common cases for every migrated feature.

| ID | Scenario | Expected acceptance result |
|---|---|---|
| UAT-COM-001 | Open the feature from navigation, search, favorites, and keyboard navigation. | Correct view, title, state, focus, and scroll behavior; no duplicate view or stale state. |
| UAT-COM-002 | Use the feature at default, minimum, restored, and maximized window sizes. | Required controls remain reachable; no unintended clipping or overlap. |
| UAT-COM-003 | Compare deterministic Avalonia renders with the approved migration baseline at identical dimensions. | Information hierarchy and state are equivalent; deviations are corrected or approved. |
| UAT-COM-004 | Enter valid, boundary, empty, and invalid values. | Validation timing, message, highlighting, and command availability are understandable and equivalent. |
| UAT-COM-005 | Cancel every picker, dialog, confirmation, and long operation. | No output mutation, leaked progress state, stale lock, or misleading success message. |
| UAT-COM-006 | Trigger an expected service failure. | Actionable error is shown; application remains usable; partial output is absent or recoverable. |
| UAT-COM-007 | Run the same input/options against the approved migration baseline and Avalonia. | Outputs are semantically equivalent and differences are explained. |
| UAT-COM-008 | Start a destructive operation. | Backup is created or offered according to policy before mutation; recovery is proven. |
| UAT-COM-009 | Save, close, reopen, and reload the feature project. | User-entered state round-trips without silent loss; legacy files remain compatible. |
| UAT-COM-010 | Switch feature/theme/map while work is active or state is dirty. | Prompts and cancellation are correct; no cross-feature state corruption. |
| UAT-COM-011 | Execute twice, including after failure or cancellation. | Commands and progress reset correctly; resources are reusable and not duplicated. |
| UAT-COM-012 | Close the application during idle and active states. | Autosave, cancellation, cleanup, and shutdown behavior match the declared policy. |
| UAT-COM-013 | Operate primarily by keyboard. | Logical tab order, visible focus, default/cancel actions, shortcuts, and accessible names are usable. |
| UAT-COM-014 | Repeat with offline/unavailable integrations. | Portable functionality remains available and Windows-only dependencies fail explicitly. |

## 10. Shell and shared-workflow cases

| ID | Area | Minimum acceptance scenarios |
|---|---|---|
| UAT-SHL-001 | Startup | First launch, normal launch, migrated profile, corrupt profile, second instance if supported, and offline launch. |
| UAT-SHL-002 | Navigation | Browse all registered features; search partial/exact names; clear search; change favorite; restore favorite after restart. |
| UAT-SHL-003 | Window state | Move, resize, maximize, minimize, restore, close, relaunch, high DPI, and disconnected-monitor bounds. |
| UAT-SHL-004 | Theme | Switch theme, inspect dialogs/custom controls, restart, and verify persistence without unreadable content. |
| UAT-SHL-005 | Notifications | Success, warning, error, queued messages, long text, repeated messages, and dismissal. |
| UAT-SHL-006 | Current maps | Pick one/many maps, use recent map, remove/move a recent file, drag/drop, query current osu! map, and cancel. |
| UAT-SHL-007 | Backup/undo | Automatic backup, manual backup/open, QuickUndo, missing backup, locked file, and failed restore. |
| UAT-SHL-008 | Project lifecycle | New/Open/Save/Save As/autosave, cancel, overwrite, legacy JSON, malformed JSON, and feature-specific extra menu items. |
| UAT-SHL-009 | Execution | Validation, start, progress, cancellation, success, recoverable error, unexpected error, retry, and editor reload. |
| UAT-SHL-010 | QuickRun/hotkeys | Manual QuickRun, smart target, no eligible target, conflicting hotkey, global enable/disable, BetterSave, QuickUndo, and focus in another app. |
| UAT-SHL-011 | Files/platform | File/folder pickers, unicode/long path, reveal in Explorer, clipboard, link launch, denied path, and cancelled picker. |
| UAT-SHL-012 | Dialogs | Confirm/cancel, default button, Escape, owner/modal behavior, validation, long content, keyboard operation, and nested error. |

## 11. Feature acceptance matrix

Every row also inherits the common cases in section 9 and all applicable shell cases in section 10.

### Wave 3: shell and preferences

| Feature | Minimum feature-specific scenarios | Required evidence |
|---|---|---|
| Get started | Empty and populated recent lists; help/website actions; live GitHub changelog; keyboard and narrow-window layout. | Paired renders, release-note Markdown, GitHub-unavailable fallback. |
| Preferences | Default and migrated settings; each path picker; invalid/missing paths; backup policy; EditorReader toggle; theme; favorites; hotkeys; smart targets; restart persistence. | Settings before/after, screenshots, validation results. |
| Current-map and backup workflows | One/many maps, current editor map, recent maps, drag/drop, missing map, backup creation/open/restore, QuickUndo, failed write. | Workspace state log, backup contents, restored-file comparison. |
| Project lifecycle | New/Open/Save/Save As/autosave for each participating feature; legacy schema; unknown fields; invalid JSON; cancelled and failed write. | Project compatibility report and round-trip files. |
| QuickRun/hotkeys | Manual run before global hooks; smart selection; target ambiguity; disabled hook; conflicting keys; repeated trigger; BetterSave/QuickUndo/reload. | Command trace, observed global behavior, cleanup confirmation. |

### Wave 4: first vertical slices

| Feature | Minimum feature-specific scenarios | Required evidence |
|---|---|---|
| Rhythm Guide | Combine one/many source maps; existing versus new guide; empty/invalid inputs; save project; resize/pop out auxiliary window; cancel before write. | Output map semantic diff, project round trip, window renders. |
| Timeline control | Empty, single, dense and overlapping markers; scroll/zoom if supported; select/navigate; boundary timestamps; resize/theme. | Render set and navigation results. |
| Auto-fail Detector | Default and overridden AR/OD; no findings, one finding, many findings; selected/current map; timeline navigation; QuickRun; editor unavailable. | Finding list parity and timeline screenshots. |
| Map Cleaner | Every cleanup option separately and representative combinations; no-op map; malformed timing; cancel; backup; editor reload; repeat run. | Before/after semantic diffs and successful restore. |

### Wave 5: conventional beatmap tools

| Feature | Minimum feature-specific scenarios | Required evidence |
|---|---|---|
| Metadata Manager | Import metadata; edit/apply to one and many difficulties; unicode; missing media; reusable configuration; partial failure; cancel. | Metadata section diffs and project round trip. |
| Property Transformer | Multiplier/offset independently and together across timing points, hit objects, bookmarks, and storyboard samples; negative/zero/boundary values; selected scopes. | Section-by-section semantic diffs. |
| Timing Copier | Preserve spacing, resnap objects, resnap bookmarks, leave objects fixed; different beat divisors; multiple targets; cancel and partial-write failure. | Timing/object/bookmark diffs per option. |
| Timing Helper | Each marker source; BPM adjustment, redline insertion, both, no valid markers, boundary timestamps, QuickRun, backup and reload. | Timing diffs and marker expectation table. |

### Wave 6: slider foundation

| Feature | Minimum feature-specific scenarios | Required evidence |
|---|---|---|
| Slider Completionator | Duration only, length only, both, preserved values, one/many selected sliders, non-slider selection, invalid duration/length, QuickRun. | Slider timing/path semantic comparison. |
| Slider Merger | Supported slider/circle orders and path types; linear connections; one/invalid selection; reversed geometry; QuickRun; repeat run. | Control-point/path visualization and `.osu` diff. |
| Slider Picturator | Supported image types/sizes; color, resolution, quality, distortion/render options; transparent image; invalid/large image; CPU/GPU paths where supported. | Generated slider render, path metrics, output diff, resource cleanup. |

### Wave 7: hitsound, colour, and mapset workflows

| Feature | Minimum feature-specific scenarios | Required evidence |
|---|---|---|
| Hitsound Preview Helper | Position/zone mapping; empty and overlapping zones; existing hitsounds; auxiliary Rhythm Guide workflow; QuickRun; missing editor selection. | Hitsound event diff and window evidence. |
| Hitsound Copier | Overwrite-all and replace-defined modes; normal/whistle/finish/clap; custom indices; storyboard samples; missing source sample; multiple targets. | Hitsound/sample/storyboard semantic diff. |
| Combo Colour Studio | Add/edit/reorder/delete colours and points; import colours/hax; time-based and single-combo points; project persistence; invalid colour. | Colour section diff, project round trip. |
| Mapset Merger | No conflict and every filename conflict policy; shared media; storyboard; unicode; missing/locked file; cancelled conflict resolution; destination cleanup after failure. | Disposable-directory manifests and merged-map validation. |

### Wave 8: visual editors and collections

| Feature | Minimum feature-specific scenarios | Required evidence |
|---|---|---|
| Object visualizer | Circles and sliders; empty/invalid objects; markers; progress ball; anchors; scaling; resizing; and theme changes. | Deterministic control behavior and coordinate checks. |
| Pattern Gallery | Import from map/code/file/ZIP; organize collection; duplicate/conflicting names; preview; transformed placement; overwrite policies; project extra menus/autosave. | Collection manifests, preview renders, placed-pattern diff. |
| Graph/value editor | Constant/graph switch; add/move/delete anchors; snapping; interpolation types; bounds; keyboard/pointer capture; zoom/resize; derivative/integral-driven consumers; save/load. | Interaction checklist, sampled curve values, graph renders. |
| Sliderator | Position and velocity curves; imported selection; slider/stream modes; optimization choices; preview; invalid graph; QuickRun; save/load. | Sampled curve/output path comparison and preview renders. |
| Tumour Generator 2 | Templates; layers; wrapping/sidedness; constant and graph parameters; preview; boundary path lengths; invalid configuration; QuickRun; async cancellation. | Generated path comparison, preview renders, project round trip. |

### Wave 9: audio studio

| Feature | Minimum feature-specific scenarios | Required evidence |
|---|---|---|
| Audio services | Decode supported formats; unsupported/corrupt file; playback/pause/stop/seek; no device; repeated open/close. | Audio-service logs and handle cleanup. |
| Hitsound Studio | Layer import/reload from beatmap, MIDI, samples and SF2; edit/reorder; preview; effects/generation; schema persistence; export difficulty/package; cancel each stage; missing source; overwrite. | Human listening sign-off, generated-file manifest, schema/project round trip, export diff. |

### Wave 10: Windows-specialized runtime

| Feature | Minimum feature-specific scenarios | Required evidence |
|---|---|---|
| Geometry Dashboard | osu! absent/present; editor closed/open; selected objects; each generator; project/save slots; preferences; activation key; cursor snap; overlay alignment at different DPI/window positions; monitor changes; graceful disconnect. | Screen recording, coordinate samples, overlay screenshots, hook/process cleanup. |

### Wave 11: release lifecycle

| Feature | Minimum feature-specific scenarios | Required evidence |
|---|---|---|
| Updater | No update, update available, skip, download progress, offline, corrupt/interrupted package, insufficient permission, apply/restart, rollback. | Version/install manifest, logs, rollback result. |
| Packaging | Clean install, upgrade from legacy, uninstall, retained user data, file associations/shortcuts if supported, each RID/architecture. | Install/uninstall logs and filesystem manifest. |

## 12. Wave acceptance gates

| Wave | Integrated acceptance condition |
|---|---|
| 0: Baseline | Users approve representative fixtures and recorded legacy outcomes. Every destructive feature has at least one trusted before/after example. |
| 1: Domain foundation | Existing user files round-trip without semantic change; math, slider, and hitsound-domain behavior remains stable. |
| 2: Services/adapters | Legacy-compatible settings/projects load; current-map, editor, backup, cancellation, progress, and manual execution workflows work through abstractions. |
| 3: Shell/common UI | A mapper can configure, select a map, navigate, manage a project, run/cancel a command, recover a backup, and restart without losing state. |
| 4: First slices | A complete savable feature and a complete QuickRun/destructive feature pass end-to-end, including timeline, backup, cancellation, and reload. |
| 5: Conventional tools | Metadata/property/timing outputs match approved legacy outcomes across representative fixtures. |
| 6: Slider foundation | Selection, slider geometry, generated paths, images, and related error handling are accepted. |
| 7: Hitsound/colour/mapset | Non-studio hitsound semantics, colour projects, and mapset conflict handling pass fixture and usability review. |
| 8: Visual editors | Pointer interaction, previews, graph mathematics, collections, Sliderator, and Tumour workflows are accepted at supported DPI/window sizes. |
| 9: Audio | Import, playback, generation, export, disposal, and perceived audio results are accepted by a human tester. |
| 10: Windows runtime | Live editor, process, global input, cursor, and overlay workflows are stable and fail safely when unavailable. |
| 11: Cutover | Packaging, updater, parity audit, rollback, and legacy user-data compatibility pass; release owner signs off. |

## 13. Visual acceptance procedure

For every migrated view state that materially affects layout:

1. Use deterministic data and the same logical dimensions.
2. Render the Avalonia view and compare it with the stored migration baseline using `$render-desktop-view`.
3. Inspect both images for hierarchy, alignment, spacing, typography, colors, wrapping, clipping, scroll affordances, enabled state, empty state, validation, and focus/default cues.
4. Exercise the same state in a real desktop session for hover, focus, pointer capture, animation, dialogs, native window behavior, DPI, and accessibility.
5. Classify every difference as defect, approved framework difference, or approved redesign.
6. Attach paired images and the decision to the UAT record.

Pixel equality is not required. Missing information, unreachable controls, misleading hierarchy, clipping, incorrect state, or materially degraded usability is a failure.

## 14. Compatibility and data-safety procedure

For every persisted format or destructive workflow:

1. Copy the original fixture into a run-specific working directory.
2. Load the legacy format in Avalonia without pre-conversion where compatibility is promised.
3. Save without changes and compare semantic content.
4. Modify representative values, save, reopen in Avalonia, then open in the legacy application when backward compatibility is promised.
5. Execute the transformation in Avalonia from identical originals and compare it with the approved legacy outcome.
6. Compare parsed models and user-visible outcomes, not only raw text.
7. Verify backup creation precedes the first mutation.
8. Cancel and inject a failure; confirm the original remains intact and partial output is cleaned up or clearly recoverable.
9. Restore the backup and prove equivalence to the original.

Any silent data loss, unrecoverable corruption, or mutation before backup is a severity 1 failure.

## 15. Defect classification

| Severity | Definition | Gate effect |
|---|---|---|
| 1 — Critical | Data corruption/loss, unsafe overwrite, security issue, unrecoverable crash, updater/rollback failure, or operation affects unintended files. | Stops all affected UAT and release. |
| 2 — Major | Core workflow unavailable or materially incorrect; output differs semantically; backup/cancel/error path broken; required platform integration fails. | Feature/wave/release cannot pass. |
| 3 — Moderate | Workaround exists but usability, parity, accessibility, layout, or secondary behavior is materially degraded. | Requires fix or explicit acceptance with owner and target release. |
| 4 — Minor | Cosmetic or low-impact difference that does not impede the workflow. | May pass when documented and accepted. |

Legacy defects reproduced intentionally must be labeled separately. Reproducing a known defect is not automatically acceptable when it threatens data safety.

## 16. Evidence and test record

Each executed case records:

- UAT case ID and feature/wave;
- candidate commit and build/package identifier;
- tester and date;
- operating system, architecture, display scaling, theme, and relevant osu!/audio state;
- fixture paths and untouched-original copies or hashes where byte-level preservation matters;
- preconditions and exact options;
- expected result, actual result, and pass/fail/blocked status;
- WPF/Avalonia screenshots or recordings when visual or interactive;
- output files, semantic comparison report, backup and restore evidence;
- logs and error messages for failure cases;
- defect IDs and approved deviations;
- feature owner and acceptance tester signatures.

Recommended result values: `Not run`, `Pass`, `Pass with accepted deviation`, `Fail`, `Blocked`, `Not applicable`.

## 17. Exit criteria

### Feature acceptance

- All applicable common, shell, and feature-specific cases have been executed.
- All severity 1 and 2 defects are closed and successfully retested.
- Severity 3/4 deviations are documented with an owner decision.
- WPF/Avalonia output is semantically equivalent or the behavior change is explicitly approved.
- Backup, cancellation, error, retry, persistence, and cleanup paths pass.
- Visual evidence is inspected; real desktop interaction is tested where a render is insufficient.
- The feature owner and acceptance tester sign off.

### Wave acceptance

- Every feature in the wave has passed its feature gate.
- The integrated wave condition in section 12 passes.
- Earlier accepted workflows have completed a focused regression pass.
- Shared services introduced by the wave have no unresolved cross-feature severity 1/2 defects.

### Release acceptance

- Every user-visible feature is accepted or has an explicitly approved deprecation.
- No severity 1 or 2 defect remains; severity 3/4 issues are published as known issues where relevant.
- Settings, projects, collections, maps, exports, backups, and updates meet their compatibility commitments.
- Clean install, upgrade from legacy data, updater, rollback, and uninstall pass.
- All claimed platforms and architectures pass their required matrix.
- Resource cleanup is verified for audio, file handles, global hooks, overlays, processes, and temporary files.
- The release owner signs the final acceptance record before changing the default executable.

## 18. Regression policy after acceptance

- Add every accepted fixture comparison and reproducible defect to automated regression coverage where practical.
- Re-run feature UAT when its observable behavior, persistence, service contract, or major Avalonia view structure changes.
- Re-run affected wave integration cases when a shared subsystem changes.
- Re-run complete release UAT for packaging, updater, framework-version, runtime-identifier, or legacy-data migration changes.
- The legacy build was kept available through step 48; step 49 records its explicitly approved removal after parity, release, and compatibility validation.

## 19. UAT case template

```text
Case ID:
Feature / wave:
Candidate commit and build:
Tester / date:
Environment:
Fixture IDs and original hashes:
Preconditions:

Steps:
1.
2.
3.

Expected result:
Actual result:
Status: Not run | Pass | Pass with accepted deviation | Fail | Blocked | N/A
Evidence paths/links:
Defect or deviation IDs:
Cleanup/recovery verified:
Feature owner decision:
Acceptance tester sign-off:
```

## 20. Final acceptance statement

The Avalonia migration is accepted for release only when the evidence demonstrates that users can safely complete all retained Mapping Tools workflows, existing user data remains usable, destructive operations remain recoverable, platform-specific behavior is truthful and stable, and updater rollback to the previous Avalonia release remains possible.

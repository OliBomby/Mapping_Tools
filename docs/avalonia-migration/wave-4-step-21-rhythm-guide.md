# Wave 4, step 21: Rhythm Guide

Status: implemented, 2026-08-06.

## Scope delivered

Rhythm Guide is the first complete mapping-tool slice in the Avalonia shell.
It supports ordered selection of one or more source beatmaps; new-map and
add-to-map exports; all legacy ruleset, name, new-combo, and event-selection
options; cancellable execution and progress; current-map shortcuts; and native
Open/Save pickers. New, Open, Save As, and legacy-named autosave projects use
the shell's conditional Project menu.

A resizable, modeless auxiliary window presents the same view model and project
state as the shell surface. `IRhythmGuideWindowService` owns that interaction;
the view model does not construct, own, or search for an Avalonia `Window`.
The service keeps one owner-linked window per view-model instance and can be
reused when Hitsound Preview Helper is migrated in step 32.

## Core algorithm and legacy parity

`RhythmGuideGenerator` and `RhythmGuideOptions` now live in Core. The generator
accepts parsed `Beatmap` objects and has no filesystem, editor-reader, process,
dialog, view, or UI-framework dependency. It preserves all four legacy
selection modes and both export modes.

The accepted Wave 0 fixture is an executable regression test. Appending the
feature-rich source to the complicated target produces exactly 1,344 total hit
objects, 1,336 circles, and 1,334 new-combo objects while retaining all nine
target timing points. The legacy implementation evaluated beat-divisor resnap
without using the returned timestamp; the extracted generator deliberately
retains that observed behavior. WPF Rhythm Guide now delegates to the same Core
implementation, avoiding two independent algorithms.

## Loading, persistence, and backup behavior

`RhythmGuideService` opens every source with `PreferLive`, matching the legacy
Editor Reader preference and disk fallback. Add-to-map opens the target
separately, transforms it, and saves it through `IBeatmapEditingGateway`.
Overwriting an existing new-map destination also uses the gateway. A genuinely
new destination is written directly because no prior file exists to protect.

Before opening any source, Avalonia now submits every ordered source path to the
preference-respecting backup service, matching WPF's generation contract. The
editing gateway still makes its mandatory safety copy when an existing export
target is overwritten, so source compatibility and destination safety are both
preserved.

The form keeps the typed source-path array and uses a two-way `|` text converter
only at the binding edge. Export Browse always opens an osu! beatmap picker in
both modes. New-map completion remains silent and does not reveal the output;
Add-to-map reports `Done!`. The shell and auxiliary window share the same form,
and the auxiliary window restores the WPF borderless 35-pixel chrome, one-pixel
border, ten-pixel content inset, drag, double-click maximize/restore, and
five-pixel resize affordances.

Existing project files remain compatible. The serializer maps legacy
`RhythmGuideVm` and nested `RhythmGuideGeneratorArgs` type metadata to the new
Application/Core models and writes those names on new saves. The Wave 0 fixture
round-trips its paths, ruleset, modes, name, and concrete beat divisors. Loaded
state is validated before installation, dirty state requires discard
confirmation, and an asynchronous autosave load cannot overwrite newer edits.

## Automated and build coverage

All 57 Core tests pass, including accepted semantic counts and new-map metadata
behavior. All 175 platform tests pass, covering legacy project aliases,
live-aware loading, target backup routing, destination writes, multi-file picker
ordering, execution/progress/output reveal, discard confirmation, and DI.

All 3 architecture tests pass. The full Release solution builds, including both
frontends and renderer tools. Its 12 warnings are pre-existing legacy
package/SDK and analyzer warnings; Avalonia Desktop builds with zero warnings
and zero errors. Boundary searches keep UI framework and MVVM dependencies out
of Core and Application.

Per the user's explicit instruction, no PNG render, image comparison, native
interaction pass, or other visual validation was performed. AXAML and compiled
bindings were validated by compilation.

## Deferred behavior

The shared Timeline control remains step 22. Rhythm Guide does not use a
timeline, so no placeholder was added. Auto-fail Detector will become the first
QuickRun-capable migrated feature after that control is available.

## Documentation consulted

- <https://docs.avaloniaui.net/docs/how-to/window-how-to>
- <https://docs.avaloniaui.net/api/avalonia/controls/window>
- <https://docs.avaloniaui.net/docs/services/file-dialogs>
- <https://docs.avaloniaui.net/docs/services/storage/file-picker-options>
- <https://docs.avaloniaui.net/docs/data-binding/compiled-bindings>
- <https://docs.avaloniaui.net/docs/migration/wpf/data-templates>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>
- <https://www.nuget.org/packages/Avalonia/12.1.0>

The local Avalonia 12.1.0 reference assemblies were also checked for modeless
`Window.Show(owner)`, activation, owner-centered startup, compiled bindings,
and the controls used by the feature form.

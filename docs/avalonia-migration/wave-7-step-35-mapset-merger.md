# Wave 7, Step 35: Mapset Merger

Status: implemented as a bounded Core/Application/Infrastructure/Desktop slice.

The migration preserves the legacy Mapset Merger workflow:

- multiple recursive mapset inputs with ordered, case-insensitive duplicate-name resolution;
- beatmap and external storyboard parsing, metadata/difficulty conflict resolution, and reference rewriting;
- audio, custom hitsound, image, video, storyboard, and sample copying with legacy extension precedence;
- beatmap-owned and storyboard-owned asset references, including nested directories, child events, and animation frames;
- the legacy Move storyboard-to-beatmap layer behavior;
- validation, cancellation, aggregate progress, staged output, commit-time rollback, and safe relative paths;
- Avalonia project persistence and legacy `Mapping_Tools.Viewmodels.MapsetMergerVm` JSON aliases; and
- the WPF implementation remains unchanged and runnable.

The pure conflict/reference rules live in `Mapping_Tools.Core`. The Application
service loads disk-only documents through the editing gateway and uses file/text
ports. Infrastructure provides recursive local-file discovery and a disposable
staging transaction that restores overwritten output on failure. The legacy tool
was export-only and did not create source backups, so no source backup behavior is
invented; the new export transaction supplies rollback for partial destination
mutation.

No QuickRun command was added because the legacy Mapset Merger view model did not
implement the legacy QuickRun contract. Manual execution and project lifecycle
are available in the Avalonia shell.

The Avalonia add action uses the shell's selected beatmap like the ordinary WPF
action; Shift invokes the current osu! editor lookup. The view preserves the
legacy lost-focus updates for editable grid cells and export-path fields.

Focused disposable-fixture coverage includes duplicate mapset/difficulty and
asset conflicts, custom sample remapping, reference rewriting, cancellation
without destination mutation, Core conflict rules, and legacy project JSON
round trips.

Avalonia migration references consulted:

- https://docs.avaloniaui.net/docs/data-binding/binding-validation
- https://docs.avaloniaui.net/docs/data-binding/compiled-bindings
- https://docs.avaloniaui.net/docs/services/file-dialogs
- https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- https://docs.avaloniaui.net/docs/migration/wpf
- https://docs.avaloniaui.net/docs/data-binding/how-to-create-a-custom-data-binding-converter
- https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.0
- https://www.nuget.org/packages/Avalonia/12.1.0
- https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/generators/observableproperty
- https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/generators/relaycommand

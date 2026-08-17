# Wave 4, step 32: Hitsound Preview Helper

Status: implemented, 2026-08-17.

## Scope delivered

Hitsound Preview Helper now has a shared Core transformation, an Application
service, and an Avalonia presentation model and view. It places the nearest
zone's hitsound on timeline events belonging to selected, bookmarked, time
queried, or all objects. Wildcard coordinates, explicit sample filenames,
sample and addition sets, custom indices, zero preview volume, slider edges,
spinner ends, and mania selection coordinates retain the legacy behavior.

The application service opens selected-object runs through the live-editor
gateway, prefers live state for the other modes, and saves through the shared
backup-aware gateway. The desktop feature retains empty-zone and time-mode
validation, ordinary runs, QuickRun, project snapshots, autosave metadata,
and legacy JSON type aliases. The legacy WPF project remains unchanged and
continues to build.

## Auxiliary-window boundary

The view model opens Rhythm Guide only through `IRhythmGuideWindowService`.
The Avalonia window service continues to own modeless window creation,
ownership, and lifetime, so Hitsound Preview Helper does not construct or
search for an Avalonia `Window`.

## Intentional platform substitution

The WPF form uses an editable `DataGrid`. The Avalonia view uses the matching
`Avalonia.Controls.DataGrid` 12.1.0 package and Material.Avalonia.DataGrid
styles. Text columns use `PropertyChanged` source updates, the checkbox column
preserves row selection, and template columns provide the enum editors while
retaining sorting and column resizing. Shift-click add is handled at the
Avalonia view boundary and delegates to the view model command; object
selection and map mutation remain outside the view.

No audio or editor process calls were added to Core or Application. The
platform-specific live-editor gateway and existing editor reload service
remain the only runtime boundaries for those effects.

## Verification

Focused Hitsound Preview Helper tests pass in Core, Application, Infrastructure,
and Desktop. The full Application, Infrastructure, and Desktop test projects
pass. The full Core suite still has four unrelated pre-existing fixture/hash
and newline failures. Both the Avalonia Desktop project and the legacy WPF
project build successfully; the WPF build reports only its existing SDK,
package, and analyzer warnings.

## Documentation consulted

- <https://docs.avaloniaui.net/docs/xaml/directives>
- <https://docs.avaloniaui.net/controls/primitives/window>
- <https://docs.avaloniaui.net/docs/input-interaction/pointer>
- <https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.0>
- <https://docs.avaloniaui.net/controls/data-display/structured-data/datagrid/>
- <https://docs.avaloniaui.net/docs/how-to/datagrid-how-to>
- <https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/generators/observableproperty>

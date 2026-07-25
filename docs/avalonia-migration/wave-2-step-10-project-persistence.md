# Wave 2, step 10: typed project persistence

Status: implemented, 2026-07-25.

## Scope delivered

The Application layer now owns the project lifecycle independently of a view:

- `ProjectDefinition<TProject>` binds a feature's typed project model to its
  legacy autosave filename, default project folder, and clean-project factory;
- `IProjectService` provides New, Open, Save, Save As, primary autosave, and
  ordered additional autosave targets;
- `IProjectStore` separates filesystem durability from use-case orchestration;
- `IProjectSerializer` separates the legacy document format from both the
  feature and its presentation state;
- `ProjectOpenResult<TProject>` returns deserialized data and its source path
  without installing either into a control or view model.

Open and Save As reuse the A8 `IFilePicker` port. Cancellation returns `null`
and performs no read or write. Serialization, filesystem, and compatibility
errors propagate to the caller so a future typed dialog/notification layer can
present them without persistence code opening a message box.

New-project confirmation remains a presentation responsibility until Wave 3
step 16 introduces typed dialogs. `CreateNew` is deliberately documented and
implemented as the operation to call only after that confirmation succeeds.

Feature-specific operations can use the explicit typed Save and Load methods
for collection import/export or auxiliary project documents. Pattern Gallery's
second recovery copy is represented by the ordered additional autosave paths
rather than an interface implemented by its view.

## Infrastructure and durability

`Mapping_Tools.Infrastructure` supplies:

- `LegacyProjectJsonSerializer`, preserving Newtonsoft type metadata, simple
  assembly names, omitted nulls, ignored reference loops, indented output, and
  the historical `{ "X": ..., "Y": ... }` `Vector2` representation;
- `FileSystemProjectStore`, which serializes before touching the destination,
  writes a unique sibling temporary file, and replaces the destination only
  after the complete UTF-8 document has been written.

An interrupted, cancelled, or failed serialization therefore leaves the
previous project file intact. Temporary files are removed on every failure
path.

The Avalonia composition root registers the serializer, store, and project
service as singletons. Future feature view models receive `IProjectService`
through constructor injection.

## Legacy compatibility

Existing project documents identify concrete CLR types with Newtonsoft
`$type` metadata and the old WPF executable assembly name `Mapping Tools`.
The compatibility binder redirects matching types that moved during Wave 1 to
`Mapping_Tools.Core`. When newly serializing a Core domain type, it writes the
old assembly name again so the current WPF release can still read the file.

The WPF `ProjectManager` remains as a compatibility facade because its current
views still implement `ISavable<T>`. Its JSON and filesystem methods now
delegate to the same Infrastructure serializer and atomic store used by the
new Application service. As individual features migrate, their Avalonia view
models should define `ProjectDefinition<TProject>` and use typed project data;
the legacy `ISavable<T>`, WPF menu-item interfaces, and facade can be removed
only after the last participating feature is accepted.

Project files containing type metadata are treated as trusted local files.
This matches legacy behavior. Newtonsoft type-name deserialization must not be
used for projects obtained from an untrusted source without a future
allow-list or explicit schema migration.

## Automated coverage

`Mapping_Tools.Platform.Tests` verifies:

- rejection of project definitions that could escape application data;
- legacy-compatible autosave and project-folder paths;
- clean-project factories without presentation-state ownership;
- primary and additional autosave ordering and duplicate suppression;
- cancelled and successful Open and Save As behavior;
- return of typed loaded data without view mutation;
- deserialization of a real Wave 0 Tumour Generator project fragment whose
  nested domain types still name the `Mapping Tools` assembly;
- legacy assembly names when writing migrated Core types;
- the historical `Vector2` JSON shape;
- tolerance of unknown properties and rejection of malformed or null JSON;
- directory creation, round trips, temporary-file cleanup, and preservation
  of an existing project after serialization failure;
- desktop DI registration and container validation.

The full solution builds, including both WPF and Avalonia frontends. This is a
service-only migration step: it changes no view or AXAML and therefore has no
visual baseline or native-dialog appearance change.

## Avalonia 12.1 documentation

No new Avalonia API was introduced in this step. Project dialogs reuse the A8
`IFilePicker` adapter already verified against:

- <https://docs.avaloniaui.net/docs/services/file-dialogs>
- <https://docs.avaloniaui.net/docs/services/storage/storage-provider>
- <https://docs.avaloniaui.net/docs/services/storage/file-picker-options>
- <https://github.com/AvaloniaUI/Avalonia/tree/12.1.0>

The full .NET Generic Host migration remains deferred to Wave 2 step 14.

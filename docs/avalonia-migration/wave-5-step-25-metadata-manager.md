# Wave 5, Step 25: Metadata Manager

Status: implemented in the Avalonia frontend.

The migrated feature preserves the legacy Metadata Manager workflow:

- import metadata from one beatmap;
- edit Unicode and ASCII metadata, tags, preview time, IDs, and colours;
- choose one or more export beatmaps using the existing file-picker port;
- create a safety copy, apply metadata, and save the metadata-derived filename;
- persist and restore `metadataproject.json` using the legacy WPF type alias;
- show the original filename and tag overflow warnings.

The WPF Material popup colour editor is represented by the Avalonia 12
ColorPicker package in the migrated view; the underlying `RgbaColour` values
and two-way editing contract are unchanged. The application currently includes
the package's Fluent style. The package also provides a Simple style and
Material colour palettes, but Material.Avalonia does not provide a native
ColorPicker style, so no unapproved Material-specific substitution was added.

Metadata export delegates filename-aware saving to the reusable
`BeatmapEditor2.SaveFileWithNameUpdate` method. Importing metadata preserves the
existing export path. Shared native picker filters now live in
`CommonFilePickerFilters`, and the shared default and compact TextBox styles no
longer impose a fixed height, allowing the wrapped Tags field to grow with its
content.

The metadata transformation lives in `Mapping_Tools.Core`, the file and backup
orchestration lives in `Mapping_Tools.Application`, and the Avalonia view only
owns form state, commands, validation, and project lifecycle. The view uses
the shared tool header, run button, progress bar, and Avalonia 12 ColorPicker.

Focused verification covers the core transformation, independent colour reads,
the multi-file view-model workflow, and legacy project deserialization and
serialization. Property Transformer, Timing Copier, and Timing Helper remain
deferred to the next migration slices.

# Wave 5, Step 26: Property Transformer

Status: implemented in the Avalonia frontend.

The migrated feature preserves the legacy Property Transformer workflow:

- multiply and offset timing-point, hit-object, bookmark, storyboard, break,
  video, and preview-time properties;
- synchronize all time fields from any time multiplier or offset field;
- optionally clip BPM, slider velocity, sample index, and volume values to the
  legacy ranges;
- apply match, mismatch, and inclusive time-range filters;
- transform beatmaps and standalone `.osb` storyboards through the shared
  editing gateway;
- create the normal backup-safe save boundary for every selected document;
- persist and restore `propertytransformerproject.json` using the legacy WPF
  type alias;
- preserve the legacy reset behavior and `Done!` completion summary.

The transformation engine lives in `Mapping_Tools.Core`, document loading and
backup-safe persistence live in `Mapping_Tools.Application`, and the Avalonia
view model owns form state, synchronized fields, project lifecycle, and
execution. The view uses the shared tool header, run button, progress bar,
compact TextBox style, invariant numeric converter, and a comma-separated
double-array converter for the filter fields.

The WPF `GroupBox`, Material switch, and floating reset button are represented
by their Avalonia/Material.Avalonia control equivalents. This is an approved
platform substitution: bindings, labels, tooltips, spacing, reset semantics,
and validation behavior remain aligned with the WPF source. Scrolling remains
shell-owned, matching the existing migration shell contract.

Focused verification covers the core transformation and filters, the
Application live-preference/save boundary, the ViewModel synchronization,
reset, execution, and filter converter, plus legacy project alias round-trip.

The implementation was checked against the Avalonia 12.1 documentation for
[binding validation](https://docs.avaloniaui.net/docs/data-binding/binding-validation),
[custom binding converters](https://docs.avaloniaui.net/docs/data-binding/how-to-create-a-custom-data-binding-converter),
[WPF migration](https://docs.avaloniaui.net/docs/migration/wpf), and
[Avalonia 12 breaking changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes).

using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools;

/// <summary>
///     Provides the canonical application metadata for the built-in mapping tools.
/// </summary>
public static class MappingToolDefinitions
{
    /// <summary>Gets Auto-fail Detector metadata.</summary>
    public static ToolDefinition AutoFailDetector { get; } = new(
        "auto-fail-detector",
        "Auto-fail Detector",
        "Detect incorrect object loading in overlapping patterns.",
        ["auto fail", "2b", "unloading", "objects"],
        QuickRunTargets.Always);

    /// <summary>Gets Map Cleaner metadata.</summary>
    public static ToolDefinition MapCleaner { get; } = new(
        "map-cleaner",
        "Map Cleaner",
        "Rebuild useful greenlines and optionally resnap map content.",
        ["clean", "greenline", "resnap", "samples"],
        QuickRunTargets.Always);

    /// <summary>Gets Rhythm Guide metadata.</summary>
    public static ToolDefinition RhythmGuide { get; } = new(
        "rhythm-guide",
        "Rhythm Guide",
        "Make a beatmap with circles from the rhythm of multiple maps.",
        ["rhythm", "hitsound", "guide", "reference"]);

    /// <summary>Gets Hitsound Preview Helper metadata.</summary>
    public static ToolDefinition HitsoundPreviewHelper { get; } = new(
        "hitsound-preview-helper",
        "Hitsound Preview Helper",
        "Place provisional hitsounds from positional zones.",
        ["hitsound", "preview", "zone", "sample", "position"],
        QuickRunTargets.Always);

    /// <summary>Gets Hitsound Studio metadata.</summary>
    public static ToolDefinition HitsoundStudio { get; } = new(
        "hitsound-studio",
        "Hitsound Studio",
        "Import, edit, preview, generate, and export hitsound layers.",
        ["hitsound", "studio", "sample", "MIDI", "SoundFont", "export", "layer"],
        QuickRunTargets.Always);

    /// <summary>Gets Hitsound Copier metadata.</summary>
    public static ToolDefinition HitsoundCopier { get; } = new(
        "hitsound-copier",
        "Hitsound Copier",
        "Copy hitsounds, samples, and storyboard sounds between beatmaps.",
        ["hitsound", "copy", "sample", "storyboard", "mute", "multi-map"],
        QuickRunTargets.Always);

    /// <summary>Gets Metadata Manager metadata.</summary>
    public static ToolDefinition MetadataManager { get; } = new(
        "metadata-manager",
        "Metadata Manager",
        "Edit metadata once and apply it to multiple beatmaps.",
        ["metadata", "artist", "title", "tags", "colours"]);

    /// <summary>Gets Property Transformer metadata.</summary>
    public static ToolDefinition PropertyTransformer { get; } = new(
        "property-transformer",
        "Property Transformer",
        "Multiply and add to timing, object, bookmark, and storyboard properties.",
        ["properties", "transform", "timing", "offset", "multiplier"]);

    /// <summary>Gets Timing Copier metadata.</summary>
    public static ToolDefinition TimingCopier { get; } = new(
        "timing-copier",
        "Timing Copier",
        "Copy timing between beatmaps with optional object resnapping.",
        ["timing", "copy", "resnap", "beat divisors", "multi-map"]);

    /// <summary>Gets Timing Helper metadata.</summary>
    public static ToolDefinition TimingHelper { get; } = new(
        "timing-helper",
        "Timing Helper",
        "Adjust BPM and add redlines so selected markers become snapped.",
        ["timing", "redlines", "BPM", "markers", "beat divisors"],
        QuickRunTargets.Always);

    /// <summary>Gets Slider Completionator metadata.</summary>
    public static ToolDefinition SliderCompletionator { get; } = new(
        "slider-completionator",
        "Slider Completionator",
        "Change slider length and duration while calculating slider velocity.",
        ["slider", "completion", "duration", "length", "velocity"],
        QuickRunTargets.AnySelection);

    /// <summary>Gets Slider Merger metadata.</summary>
    public static ToolDefinition SliderMerger { get; } = new(
        "slider-merger",
        "Slider Merger",
        "Merge selected sliders and circles into one connected slider.",
        ["slider", "merge", "bezier", "connection", "circles"],
        QuickRunTargets.MultipleSelection);

    /// <summary>Gets Slider Picturator metadata.</summary>
    public static ToolDefinition SliderPicturator { get; } = new(
        "slider-picturator",
        "Slider Picturator",
        "Generate a slider path that reproduces an imported image.",
        ["slider", "picture", "image", "picturator", "render"],
        QuickRunTargets.AnySelection);

    /// <summary>Gets Sliderator metadata.</summary>
    public static ToolDefinition Sliderator { get; } = new(
        "sliderator",
        "Sliderator",
        "Create variable-velocity sliders and streams from an editable graph.",
        ["slider", "sliderator", "variable velocity", "stream", "graph", "SV"],
        QuickRunTargets.SingleSelection);

    /// <summary>Gets Tumour Generator metadata.</summary>
    public static ToolDefinition TumourGenerator { get; } = new(
        "tumour-generator",
        "Tumour Generator 2",
        "Generate copious amounts of tumours on sliders.",
        ["tumour", "tumor", "slider", "layers", "graph", "templates"],
        QuickRunTargets.AnySelection);

    /// <summary>Gets Combo Colour Studio metadata.</summary>
    public static ToolDefinition ComboColourStudio { get; } = new(
        "combo-colour-studio",
        "Combo Colour Studio",
        "Customize combo-colour sequences, bursts, and colour haxing.",
        ["combo", "colour", "color", "hax", "palette", "burst"],
        QuickRunTargets.Always);

    /// <summary>Gets Mapset Merger metadata.</summary>
    public static ToolDefinition MapsetMerger { get; } = new(
        "mapset-merger",
        "Mapset Merger",
        "Combine multiple mapsets and resolve beatmap, audio, image, storyboard, and sample conflicts.",
        ["mapset", "merge", "audio", "image", "storyboard", "samples", "conflicts"]);

    /// <summary>Gets Pattern Gallery metadata.</summary>
    public static ToolDefinition PatternGallery { get; } = new(
        "pattern-gallery",
        "Pattern Gallery",
        "Collect, preview, organize, and place reusable hit-object patterns.",
        ["pattern", "gallery", "collection", "osu", "snippet"],
        QuickRunTargets.Always);

    /// <summary>Gets Geometry Dashboard metadata.</summary>
    public static ToolDefinition GeometryDashboard { get; } = new(
        "geometry-dashboard",
        "Geometry Dashboard",
        "Generate, display, snap to, and save useful geometry around osu! hit objects.",
        ["geometry", "snapping", "virtual objects", "overlay", "generators"]);
}

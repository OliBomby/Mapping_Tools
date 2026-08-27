using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.TumourGenerator;

/// <summary>Provides the discoverable metadata for Tumour Generator.</summary>
public static class TumourGeneratorToolDefinition
{
    /// <summary>Gets the stable Tumour Generator metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "tumour-generator",
        "Tumour Generator 2",
        "Generate copious amounts of tumours on sliders.",
        ["tumour", "tumor", "slider", "layers", "graph", "templates"],
        QuickRunTargets.AnySelection);
}

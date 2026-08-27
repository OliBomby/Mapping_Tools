using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.Sliderator;

/// <summary>Provides the discoverable metadata for Sliderator.</summary>
public static class SlideratorToolDefinition
{
    /// <summary>Gets the stable Sliderator metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "sliderator",
        "Sliderator",
        "Create variable-velocity sliders and streams from an editable graph.",
        ["slider", "sliderator", "variable velocity", "stream", "graph", "SV"],
        QuickRunTargets.SingleSelection);
}

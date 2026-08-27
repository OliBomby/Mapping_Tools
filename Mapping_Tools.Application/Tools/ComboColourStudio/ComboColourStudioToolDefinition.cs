using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.ComboColourStudio;

/// <summary>Provides the discoverable metadata for Combo Colour Studio.</summary>
public static class ComboColourStudioToolDefinition
{
    /// <summary>Gets the stable Combo Colour Studio metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "combo-colour-studio",
        "Combo Colour Studio",
        "Customize combo-colour sequences, bursts, and colour haxing.",
        ["combo", "colour", "color", "hax", "palette", "burst"],
        QuickRunTargets.Always);
}

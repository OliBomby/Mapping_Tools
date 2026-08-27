using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.SliderPicturator;

/// <summary>Provides the discoverable metadata for Slider Picturator.</summary>
public static class SliderPicturatorToolDefinition
{
    /// <summary>Gets the stable Slider Picturator metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "slider-picturator",
        "Slider Picturator",
        "Generate a slider path that reproduces an imported image.",
        ["slider", "picture", "image", "picturator", "render"],
        QuickRunTargets.AnySelection);
}

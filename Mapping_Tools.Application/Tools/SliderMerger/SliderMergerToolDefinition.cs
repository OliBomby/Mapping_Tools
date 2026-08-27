using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.SliderMerger;

/// <summary>Provides the discoverable metadata for Slider Merger.</summary>
public static class SliderMergerToolDefinition
{
    /// <summary>Gets the stable Slider Merger metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "slider-merger",
        "Slider Merger",
        "Merge selected sliders and circles into one connected slider.",
        ["slider", "merge", "bezier", "connection", "circles"],
        QuickRunTargets.MultipleSelection);
}

using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.SliderCompletionator;

/// <summary>Provides the discoverable metadata for Slider Completionator.</summary>
public static class SliderCompletionatorToolDefinition
{
    /// <summary>Gets the stable Slider Completionator metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "slider-completionator",
        "Slider Completionator",
        "Change slider length and duration while calculating slider velocity.",
        ["slider", "completion", "duration", "length", "velocity"],
        QuickRunTargets.AnySelection);
}

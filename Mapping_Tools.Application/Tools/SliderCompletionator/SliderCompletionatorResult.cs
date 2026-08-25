namespace Mapping_Tools.Application.Tools.SliderCompletionator;

/// <summary>Reports Slider Completionator's completed maps and slider count.</summary>
/// <param name="ProcessedPaths">The input paths that were opened and saved.</param>
/// <param name="SlidersCompleted">The number of selected sliders changed.</param>
public sealed record SliderCompletionatorResult(
    IReadOnlyList<string> ProcessedPaths,
    int SlidersCompleted);


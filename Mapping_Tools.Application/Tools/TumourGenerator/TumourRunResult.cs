using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.TumourGenerating;

namespace Mapping_Tools.Application.Tools.TumourGenerator;

/// <summary>Reports the number of transformed sliders across all requested maps.</summary>
/// <param name="Paths">The paths saved by the operation.</param>
/// <param name="SlidersTumourated">The number of sliders whose paths changed.</param>
/// <param name="EditorReloaded">Whether a live editor reload was requested.</param>
public sealed record TumourRunResult(
    IReadOnlyList<string> Paths,
    int SlidersTumourated,
    bool EditorReloaded);


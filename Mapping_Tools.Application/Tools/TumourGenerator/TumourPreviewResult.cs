using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.TumourGenerating;

namespace Mapping_Tools.Application.Tools.TumourGenerator;

/// <summary>Reports one completed preview generation.</summary>
/// <param name="HitObject">The independently generated preview slider.</param>
/// <param name="LayerLengths">The lengths observed at active layer boundaries.</param>
public sealed record TumourPreviewResult(
    HitObject HitObject,
    IReadOnlyList<double> LayerLengths);


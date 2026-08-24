using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.Tools.TumourGenerator.Models;

/// <summary>Reports one completed preview generation.</summary>
/// <param name="HitObject">The independently generated preview slider.</param>
/// <param name="LayerLengths">The lengths observed at active layer boundaries.</param>
public sealed record TumourPreviewResult(
    HitObject HitObject,
    IReadOnlyList<double> LayerLengths);


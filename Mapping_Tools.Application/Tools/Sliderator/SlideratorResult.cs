using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.Sliderator;

namespace Mapping_Tools.Application.Tools.Sliderator;

/// <summary>Reports the written Sliderator beatmap and generated dimensions.</summary>
/// <param name="Path">The beatmap path written by the operation.</param>
/// <param name="Applied">The Core generation result.</param>
/// <param name="EditorReloaded">Whether the live editor was requested to reload.</param>
public sealed record SlideratorResult(
    string Path,
    SlideratorApplyResult Applied,
    bool EditorReloaded);


using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Images;
using Mapping_Tools.Core.Tools.SliderPicturator;

namespace Mapping_Tools.Application.Tools.SliderPicturator;

/// <summary>Reports the generated slider and the map written by Slider Picturator.</summary>
/// <param name="Path">The beatmap path written by the operation.</param>
/// <param name="SegmentCount">The estimated slider segment count.</param>
public sealed record SliderPicturatorResult(string Path, long SegmentCount);


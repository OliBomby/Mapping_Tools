using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Application.Tools.PatternGallery;

/// <summary>Reports one completed Pattern Gallery placement.</summary>
public sealed record PatternGalleryRunResult(int PatternCount, string Message);


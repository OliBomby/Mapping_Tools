using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Application.Tools.PatternGallery;

/// <summary>Describes the physical folders owned by one Pattern Gallery collection.</summary>
public sealed record PatternGalleryCollectionPaths(
    string Root,
    string Collection,
    string PatternFiles,
    string ProjectFile);


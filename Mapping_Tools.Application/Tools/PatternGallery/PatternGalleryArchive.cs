using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Application.Tools.PatternGallery;

/// <summary>In-memory representation of a validated Pattern Gallery ZIP file.</summary>
public sealed record PatternGalleryArchive(
    string CollectionFolderName,
    string ProjectFileName,
    string ProjectJson,
    IReadOnlyList<PatternGalleryArchiveFile> PatternFiles);


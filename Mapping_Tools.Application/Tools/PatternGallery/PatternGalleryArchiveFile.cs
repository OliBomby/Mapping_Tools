using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Application.Tools.PatternGallery;

/// <summary>One pattern file included in a collection archive.</summary>
public sealed record PatternGalleryArchiveFile(string FileName, byte[] Content);


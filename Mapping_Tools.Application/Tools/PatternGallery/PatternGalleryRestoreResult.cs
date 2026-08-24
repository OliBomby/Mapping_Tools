using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Application.Tools.PatternGallery;

/// <summary>Reports a collection restore's indexed-file changes.</summary>
public sealed record PatternGalleryRestoreResult(int RemovedCount, int AddedCount);


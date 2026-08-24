namespace Mapping_Tools.Application.Tools.PatternGallery.Models;

/// <summary>Reports a collection restore's indexed-file changes.</summary>
public sealed record PatternGalleryRestoreResult(int RemovedCount, int AddedCount);


namespace Mapping_Tools.Application.Tools.PatternGallery.Models;

/// <summary>In-memory representation of a validated Pattern Gallery ZIP file.</summary>
public sealed record PatternGalleryArchive(
    string CollectionFolderName,
    string ProjectFileName,
    string ProjectJson,
    IReadOnlyList<PatternGalleryArchiveFile> PatternFiles);


using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Application.Tools.PatternGallery;

/// <summary>Abstracts safe ZIP creation, reading, and extraction.</summary>
public interface IPatternGalleryArchiveService
{
    /// <summary>Creates a collection ZIP with a project JSON entry and pattern files.</summary>
    /// <param name="archivePath">The destination ZIP path.</param>
    /// <param name="collectionFolderName">The root folder inside the archive.</param>
    /// <param name="projectFileName">The project JSON filename inside the root folder.</param>
    /// <param name="projectJson">The serialized project document.</param>
    /// <param name="patternFiles">The pattern files to include.</param>
    /// <param name="cancellationToken">Cancels before the archive is committed.</param>
    Task ExportAsync(
        string archivePath,
        string collectionFolderName,
        string projectFileName,
        string projectJson,
        IReadOnlyList<PatternGalleryArchiveFile> patternFiles,
        CancellationToken cancellationToken = default);

    /// <summary>Reads and validates a collection ZIP without extracting it.</summary>
    /// <param name="archivePath">The existing ZIP path.</param>
    /// <param name="cancellationToken">Cancels between entry reads.</param>
    /// <returns>The project entry and `.osu` files in the archive.</returns>
    Task<PatternGalleryArchive> ReadAsync(
        string archivePath,
        CancellationToken cancellationToken = default);

    /// <summary>Extracts a validated archive below the supplied collection root.</summary>
    /// <param name="archivePath">The existing ZIP path.</param>
    /// <param name="basePath">The directory below which the archive root is created.</param>
    /// <param name="cancellationToken">Cancels before the next entry is written.</param>
    Task ExtractAsync(
        string archivePath,
        string basePath,
        CancellationToken cancellationToken = default);
}


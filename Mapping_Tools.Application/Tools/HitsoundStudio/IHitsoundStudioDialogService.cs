using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Tools.HitsoundStudio;

/// <summary>Owns the feature-specific import and export forms.</summary>
public interface IHitsoundStudioDialogService
{
    /// <summary>Shows the layer import form and returns submitted values.</summary>
    /// <param name="defaultName">The initial name shown for the new layer.</param>
    /// <param name="cancellationToken">Closes the modal operation when cancellation is requested.</param>
    Task<HitsoundStudioImportRequest?> ShowImportAsync(
        string defaultName,
        CancellationToken cancellationToken = default);

    /// <summary>Shows export options initialized from the current project snapshot.</summary>
    /// <param name="project">The current project copied into the form.</param>
    /// <param name="cancellationToken">Closes the modal operation when cancellation is requested.</param>
    Task<HitsoundStudioProject?> ShowExportAsync(
        HitsoundStudioProject project,
        CancellationToken cancellationToken = default);
}


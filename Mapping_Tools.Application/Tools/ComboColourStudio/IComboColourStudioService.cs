using Mapping_Tools.Core.Tools.ComboColourStudio;

namespace Mapping_Tools.Application.Tools.ComboColourStudio;

/// <summary>Runs Combo Colour Studio imports and beatmap transformations.</summary>
public interface IComboColourStudioService
{
    /// <summary>Imports only the source beatmap's combo palette into a project.</summary>
    /// <param name="path">The beatmap file to read.</param>
    /// <param name="project">The project to update.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task ImportComboColoursAsync(
        string path,
        ComboColourProject project,
        CancellationToken cancellationToken = default);

    /// <summary>Infers palette, normal points, and burst points from a source beatmap.</summary>
    /// <param name="path">The beatmap file to read.</param>
    /// <param name="project">The project to replace.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task ImportColourHaxAsync(
        string path,
        ComboColourProject project,
        CancellationToken cancellationToken = default);

    /// <summary>Applies a project to each target map with progress reporting.</summary>
    /// <param name="paths">The target beatmap paths.</param>
    /// <param name="project">The project snapshot to apply.</param>
    /// <param name="progress">Receives a percentage from zero through one hundred.</param>
    /// <param name="cancellationToken">Cancels before the next map is opened or saved.</param>
    /// <returns>The number of successfully saved maps.</returns>
    Task<ComboColourStudioRunResult> ApplyAsync(
        IReadOnlyList<string> paths,
        ComboColourProject project,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

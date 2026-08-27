using Mapping_Tools.Core.Tools.ComboColourStudio.Models;

namespace Mapping_Tools.Application.Tools.ComboColourStudio;

/// <summary>Runs Combo Colour Studio imports and beatmap transformations.</summary>
public interface IComboColourStudioService
{
    /// <summary>Extracts only the source beatmap's combo palette.</summary>
    /// <param name="path">The beatmap file to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A new project containing the extracted palette.</returns>
    Task<ComboColourEngineOptions> ImportComboColoursAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>Extracts a palette and infers normal and burst points from a source beatmap.</summary>
    /// <param name="path">The beatmap file to read.</param>
    /// <param name="maxBurstLength">The largest combo eligible for burst points.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A new project containing the extracted palette and inferred colour points.</returns>
    Task<ComboColourEngineOptions> ImportColourHaxAsync(
        string path,
        int maxBurstLength,
        CancellationToken cancellationToken = default);

    /// <summary>Applies a project to each target map with progress reporting.</summary>
    /// <param name="paths">The target beatmap paths.</param>
    /// <param name="project">The project snapshot to apply.</param>
    /// <param name="progress">Receives normalized progress from zero through one.</param>
    /// <param name="cancellationToken">Cancels before the next map is opened or saved.</param>
    /// <returns>The number of successfully saved maps.</returns>
    Task<ComboColourStudioRunResult> ApplyAsync(
        IReadOnlyList<string> paths,
        ComboColourServiceOptions project,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

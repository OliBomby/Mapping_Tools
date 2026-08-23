using Mapping_Tools.Core.Tools.RhythmGuide;

namespace Mapping_Tools.Application.Tools.RhythmGuide;

/// <summary>Represents the complete legacy-compatible Rhythm Guide project document.</summary>
public sealed class RhythmGuideProject
{
    /// <summary>Gets or sets the generator options stored by the project.</summary>
    public RhythmGuideOptions GuideGeneratorArgs { get; set; } = new();
}

/// <summary>Reports the destination and number of guide objects produced by one run.</summary>
/// <param name="ExportPath">The generated or modified beatmap path.</param>
/// <param name="AddedObjectCount">The number of guide objects added.</param>
/// <param name="ExportMode">Whether the operation created or extended a beatmap.</param>
public sealed record RhythmGuideResult(
    string ExportPath,
    int AddedObjectCount,
    RhythmGuideExportMode ExportMode);

/// <summary>Loads source state, applies the pure generator, and persists through safety boundaries.</summary>
public interface IRhythmGuideService
{
    /// <summary>Loads the selected maps, generates guide objects, and saves the destination.</summary>
    /// <param name="options">The sources, destination, and transformation choices.</param>
    /// <param name="cancellationToken">Cancels loading, generation, or saving.</param>
    /// <returns>The destination and number of added guide objects.</returns>
    Task<RhythmGuideResult> GenerateAsync(
        RhythmGuideOptions options,
        CancellationToken cancellationToken = default);
}

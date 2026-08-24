using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;

namespace Mapping_Tools.Application.Tools.RhythmGuide;

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

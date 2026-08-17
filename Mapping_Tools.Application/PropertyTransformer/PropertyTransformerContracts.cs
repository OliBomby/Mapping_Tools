using Mapping_Tools.Core.Tools.PropertyTransformer;

namespace Mapping_Tools.Application.PropertyTransformer;

/// <summary>
/// Represents the complete Property Transformer project persisted by the shell.
/// </summary>
public sealed class PropertyTransformerProject : PropertyTransformerOptions
{
}

/// <summary>Reports the paths transformed by one Property Transformer run.</summary>
/// <param name="ProcessedPaths">The output paths in selection order.</param>
public sealed record PropertyTransformerResult(IReadOnlyList<string> ProcessedPaths)
{
    /// <summary>Gets the number of documents transformed.</summary>
    public int ProcessedCount => ProcessedPaths.Count;
}

/// <summary>
/// Runs Property Transformer over selected beatmaps and storyboards.
/// </summary>
public interface IPropertyTransformerService
{
    /// <summary>
    /// Applies the configured transformations and saves every selected document.
    /// </summary>
    /// <param name="paths">The beatmap or storyboard paths in selection order.</param>
    /// <param name="options">The multipliers, offsets, clipping, and filters to apply.</param>
    /// <param name="progress">Optional aggregate progress reporting.</param>
    /// <param name="cancellationToken">Cancels loading, transformation, backup, or saving.</param>
    /// <returns>The paths processed by the operation.</returns>
    Task<PropertyTransformerResult> TransformAsync(
        IReadOnlyList<string> paths,
        PropertyTransformerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

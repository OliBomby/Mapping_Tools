namespace Mapping_Tools.Application.Tools.PropertyTransformer;

/// <summary>
///     Runs Property Transformer over selected beatmaps and storyboards.
/// </summary>
public interface IPropertyTransformerService
{
    /// <summary>
    ///     Applies the configured transformations and saves every selected document.
    /// </summary>
    /// <param name="paths">The beatmap or storyboard paths in selection order.</param>
    /// <param name="options">The multipliers, offsets, clipping, and filters to apply.</param>
    /// <param name="progress">Optional aggregate progress reporting.</param>
    /// <param name="cancellationToken">Cancels loading, transformation, backup, or saving.</param>
    /// <returns>The paths processed by the operation.</returns>
    Task<PropertyTransformerResult> TransformAsync(
        IReadOnlyList<string> paths,
        PropertyTransformerServiceOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

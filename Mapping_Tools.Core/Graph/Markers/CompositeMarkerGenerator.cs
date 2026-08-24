using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Graph.Markers;

/// <summary>Combines multiple graph marker generators in display order.</summary>
public sealed class CompositeMarkerGenerator : IGraphMarkerGenerator
{
    /// <summary>Creates a composite marker generator.</summary>
    /// <param name="generators">The generators to invoke in order.</param>
    public CompositeMarkerGenerator(IEnumerable<IGraphMarkerGenerator> generators)
    {
        Generators = generators?.ToArray() ?? throw new ArgumentNullException(nameof(generators));
    }

    /// <summary>Gets the child generators.</summary>
    public IReadOnlyList<IGraphMarkerGenerator> Generators { get; }

    /// <inheritdoc />
    public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, GraphMarkerOrientation orientation, int maxMarkers)
    {
        foreach (var generator in Generators)
        foreach (var marker in generator.GenerateMarkers(start, end, orientation, maxMarkers))
            yield return marker;
    }
}

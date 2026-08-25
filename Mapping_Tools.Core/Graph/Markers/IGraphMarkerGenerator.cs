namespace Mapping_Tools.Core.Graph.Markers;

/// <summary>Generates visible graph markers for a value interval.</summary>
public interface IGraphMarkerGenerator
{
    /// <summary>Generates markers for an interval and display budget.</summary>
    /// <param name="start">The first visible value.</param>
    /// <param name="end">The last visible value.</param>
    /// <param name="orientation">The orientation of generated markers.</param>
    /// <param name="maxMarkers">The maximum desired marker count.</param>
    /// <returns>The generated marker sequence.</returns>
    IEnumerable<GraphMarker> GenerateMarkers(double start, double end, GraphMarkerOrientation orientation, int maxMarkers);
}


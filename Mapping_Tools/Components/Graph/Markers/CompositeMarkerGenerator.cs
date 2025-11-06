using Mapping_Tools.Annotations;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Graph.Markers;

public class CompositeMarkerGenerator : IMarkerGenerator {
    [NotNull]
    public IMarkerGenerator[] Generators { get; set; }

    public CompositeMarkerGenerator(IMarkerGenerator[] generators) {
        Generators = generators;
    }

    public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, Orientation orientation, int maxMarkers) {
        foreach (var generator in Generators) {
            foreach (var marker in generator.GenerateMarkers(start, end, orientation, maxMarkers)) {
                yield return marker;
            }
        }
    }
}
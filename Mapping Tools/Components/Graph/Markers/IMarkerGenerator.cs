using System.Collections.Generic;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Graph.Markers {
    public interface IMarkerGenerator {
        IEnumerable<GraphMarker> GenerateMarkers(double start, double end, Orientation orientation, int maxMarkers);
    }
}
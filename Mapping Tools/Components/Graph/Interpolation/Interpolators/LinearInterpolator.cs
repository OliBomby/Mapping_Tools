using Mapping_Tools.Components.Graph.Interpolation;

namespace Mapping_Tools.Components.Graph.Interpolators {
    [IgnoreInterpolator]
    public class LinearInterpolator : IGraphInterpolator {
        public string Name => "Linear";
        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            return h1 + (h2 - h1) * t;
        }
    }
}
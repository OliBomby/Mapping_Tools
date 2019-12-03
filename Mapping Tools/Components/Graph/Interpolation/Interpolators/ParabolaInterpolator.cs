using Mapping_Tools.Components.Graph.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [IgnoreInterpolator]
    public class ParabolaInterpolator : IGraphInterpolator {
        public string Name => "Parabola";

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            return h1 + (h2 - h1) * (-2 * parameter * t * t + (2 * parameter + 1) * t);
        }
    }
}
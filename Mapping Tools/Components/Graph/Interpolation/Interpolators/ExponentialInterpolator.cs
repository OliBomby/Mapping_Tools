using System;
using Mapping_Tools.Components.Graph.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [IgnoreInterpolator]
    public class ExponentialInterpolator : IGraphInterpolator {
        public string Name => "Broken exponential";

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            return h1 * Math.Pow((h2 / h1), t);
        }
    }
}
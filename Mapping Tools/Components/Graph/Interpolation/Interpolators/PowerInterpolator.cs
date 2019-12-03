using System;
using Mapping_Tools.Components.Graph.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [IgnoreInterpolator]
    public class PowerInterpolator : IGraphInterpolator {
        public string Name => "Broken Power";

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            var a = Math.Pow(h1, (1 / parameter));
            var b = Math.Pow(h2, (1 / parameter));
            return Math.Pow(((b - a) * t + a), parameter);
        }
    }
}
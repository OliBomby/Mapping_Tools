using System;
using System.ComponentModel;
using Mapping_Tools.Components.Graph.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Wave")]
    public class SineInterpolator : IGraphInterpolator {
        public string Name => "Wave";

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            return h1 + (h2 - h1) * (Math.Sin((Math.Round(parameter) * 2 + 1) * Math.PI * t - Math.PI / 2) + 1) / 2;
        }
    }
}
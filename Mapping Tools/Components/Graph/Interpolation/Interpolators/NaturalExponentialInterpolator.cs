using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Single curve")]
    public class NaturalExponentialInterpolator : IGraphInterpolator {
        private readonly LinearInterpolator linearDegenerate;

        public string Name => "Single curve";

        public NaturalExponentialInterpolator() {
            linearDegenerate = new LinearInterpolator();
        }

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            if (Math.Abs(parameter) < Precision.DOUBLE_EPSILON) {
                return linearDegenerate.GetInterpolation(t, h1, h2, parameter);
            }
            return h1 + (h2 - h1) * (Math.Exp(-parameter * t) - 1) / (Math.Exp(-parameter) - 1);
        }
    }
}
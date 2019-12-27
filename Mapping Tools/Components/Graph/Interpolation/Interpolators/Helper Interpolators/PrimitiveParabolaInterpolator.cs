using System;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators.Helper_Interpolators {
    [IgnoreInterpolator]
    public class PrimitiveParabolaInterpolator : CustomInterpolator, IDerivableInterpolator {
        public double C { get; set; }
        public double D { get; set; }

        public PrimitiveParabolaInterpolator() {
            InterpolationFunction = Function;
        }

        public double Function(double t) {
            var p = MathHelper.Clamp(P, -1, 1);
            return (t * (2 * p * Math.Pow(t, 2) * (D - C) + 3 * (p + 1) * t * (C - D) - 6 * C)) / (C * (p - 3) - D * (p + 3));
        }

        public IGraphInterpolator GetDerivativeInterpolator() {
            return new ParabolaInterpolator {P = P};
        }

        public double GetDerivative(double t) {
            var p = MathHelper.Clamp(P, -1, 1);
            return -(6 * (C * (t - 1) * (p * t - 1) + D * t * (p * -t + p + 1))) / (C * (p - 3) - D * (p + 3));
        }
    }
}
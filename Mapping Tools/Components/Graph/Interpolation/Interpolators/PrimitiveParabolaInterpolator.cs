using System;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [IgnoreInterpolator]
    public class PrimitiveParabolaInterpolator : CustomInterpolator, IDerivableInterpolator {
        public PrimitiveParabolaInterpolator() {
            InterpolationFunction = Function;
        }

        public double Function(double t, double p) {
            p = MathHelper.Clamp(p, -1, 1);
            return t * t * (p * (3 - 2 * t) + 3) / (p + 3);
        }

        public IGraphInterpolator GetDerivativeInterpolator() {
            return new ParabolaInterpolator {P = P};
        }

        public double GetDerivative(double t) {
            return -(6 * (t - 1) * t * t) / Math.Pow(P + 3, 2);
        }
    }
}
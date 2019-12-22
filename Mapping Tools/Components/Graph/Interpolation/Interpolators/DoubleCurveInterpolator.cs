using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Double curve")]
    [VerticalMirrorInterpolator]
    public class DoubleCurveInterpolator : CustomInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public string Name => "Double curve";

        public DoubleCurveInterpolator() {
            _linearDegenerate = new LinearInterpolator();
            InterpolationFunction = Function;
        }

        public double Function(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t);
            }

            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            if (t < 0.5) {
                return 0.5 * F(t * 2, p);
            }
            return 0.5 + 0.5 * F(t * 2 - 1, -p);
        }

        private static double F(double t, double k) {
            return (Math.Exp(k * t) - 1) / (Math.Exp(k) - 1);
        }
    }
}
using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Double curve 3")]
    [VerticalMirrorInterpolator]
    public class DoubleCurveInterpolator3 : CustomInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public DoubleCurveInterpolator3() {
            _linearDegenerate = new LinearInterpolator();
            InterpolationFunction = Function;
        }

        public double Function(double t, double p) {
            if (Math.Abs(p) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t);
            }

            p = MathHelper.Clamp(p, -1, 1) * 10;
            if (t < 0.5) {
                return 0.5 * F(t * 2, p);
            }
            return 0.5 + 0.5 * F(t * 2 - 1, -p);
        }

        private static double F(double t, double k) {
            return (Math.Pow(2, k) * t) / ((Math.Pow(2, k) - 1) * t + 1);
        }
    }
}
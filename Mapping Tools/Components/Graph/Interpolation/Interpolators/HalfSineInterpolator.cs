using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Half sine")]
    [VerticalMirrorInterpolator]
    public class HalfSineInterpolator : CustomInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public HalfSineInterpolator() {
            _linearDegenerate = new LinearInterpolator();
            InterpolationFunction = Function;
        }

        private double Function(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t);
            }

            var p = MathHelper.Clamp(P, -1, 1);
            if (p < 0) {
                return 1 - F(1 - t, -p);
            }
            return F(t, p);
        }

        private static double F(double t, double k) {
            var b = 2 * Math.Acos(1 / (Math.Sqrt(2) * k - k + 1));
            return Math.Sin(t * b) / Math.Sin(b);
        }
    }
}
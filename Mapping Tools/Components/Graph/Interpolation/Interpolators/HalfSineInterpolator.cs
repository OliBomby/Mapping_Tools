using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Half sine")]
    [VerticalMirrorInterpolator]
    public class HalfSineInterpolator : IGraphInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public HalfSineInterpolator() {
            _linearDegenerate = new LinearInterpolator();
        }

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            if (Math.Abs(parameter) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t, h1, h2, parameter);
            }

            var p = MathHelper.Clamp(parameter, -1, 1);
            if (p < 0) {
                return h1 + (h2 - h1) * 1 - F(1 - t, -p);
            }
            return h1 + (h2 - h1) * F(t, p);
        }

        private static double F(double t, double k) {
            var b = 2 * Math.Acos(1 / (Math.Sqrt(2) * k - k + 1));
            return Math.Sin(t * b) / Math.Sin(b);
        }
    }
}
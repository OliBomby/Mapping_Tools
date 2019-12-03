using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Double curve 2")]
    [VerticalMirrorInterpolator]
    public class DoubleCurveInterpolator2 : IGraphInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public DoubleCurveInterpolator2() {
            _linearDegenerate = new LinearInterpolator();
        }

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            if (Math.Abs(parameter) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t, h1, h2, parameter);
            }

            var p = -MathHelper.Clamp(parameter, -1, 1) * 10;
            if (t < 0.5) {
                return h1 + (h2 - h1) * 0.5 * F(t * 2, p);
            }
            return h1 + (h2 - h1) * (0.5 + 0.5 * F(t * 2 - 1, -p));
        }

        private static double F(double t, double k) {
            return (Math.Pow(2, k * t) - 1) / (Math.Pow(2, k) - 1);
        }
    }
}
using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Single curve 2")]
    [VerticalMirrorInterpolator]
    public class SingleCurveInterpolator2 : CustomInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public SingleCurveInterpolator2() {
            _linearDegenerate = new LinearInterpolator();
            InterpolationFunction = Function;
        }

        public double Function(double t, double p) {
            if (Math.Abs(p) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t);
            }

            p = -MathHelper.Clamp(p, -1, 1) * 10;
            return F(t, p);
        }

        private static double F(double t, double k) {
            return (Math.Pow(2, k * t) - 1) / (Math.Pow(2, k) - 1);
        }
    }
}
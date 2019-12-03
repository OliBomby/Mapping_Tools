using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Single curve")]
    [VerticalMirrorInterpolator]
    public class SingleCurveInterpolator : IGraphInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public SingleCurveInterpolator() {
            _linearDegenerate = new LinearInterpolator();
        }

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            if (Math.Abs(parameter) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t, h1, h2, parameter);
            }

            var p = -MathHelper.Clamp(parameter, -1, 1) * 10;
            return h1 + (h2 - h1) * F(t, p);
        }

        private static double F(double t, double k) {
            return (Math.Exp(k * t) - 1) / (Math.Exp(k) - 1);
        }
    }
}
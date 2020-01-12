using Mapping_Tools.Classes.MathUtil;
using System;
using System.ComponentModel;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Single curve")]
    [VerticalMirrorInterpolator]
    public class SingleCurveInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public SingleCurveInterpolator() {
            _linearDegenerate = new LinearInterpolator();
            InterpolationFunction = Function;
        }

        public double Function(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t);
            }

            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            return F(t, p);
        }

        private static double F(double t, double k) {
            return (Math.Exp(k * t) - 1) / (Math.Exp(k) - 1);
        }

        public double GetDerivative(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetDerivative(t);
            }

            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            return p * Math.Exp(p * t) / (Math.Exp(p) - 1);
        }

        public double GetIntegral(double t1, double t2) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetIntegral(t1, t2);
            }
            
            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            return Primitive(t2, p) - Primitive(t1, p);
        }

        private static double Primitive(double t, double p) {
            return (Math.Exp(p * t) / p - t) / (Math.Exp(p) - 1);
        }
    }
}
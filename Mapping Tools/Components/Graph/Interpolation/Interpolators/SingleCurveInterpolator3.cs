using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Single curve 3")]
    [VerticalMirrorInterpolator]
    public class SingleCurveInterpolator3 : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public SingleCurveInterpolator3() {
            _linearDegenerate = new LinearInterpolator();
            InterpolationFunction = Function;
        }

        public double Function(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t);
            }

            var p = MathHelper.Clamp(P, -1, 1) * 7;
            return F(t, p);
        }

        private static double F(double t, double k) {
            return (Math.Exp(k) * t) / ((Math.Exp(k) - 1) * t + 1);
        }

        public double GetDerivative(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetDerivative(t);
            }

            var p = MathHelper.Clamp(P, -1, 1) * 7;
            return Math.Exp(p) / Math.Pow(t * (Math.Exp(p) - 1) + 1, 2);
        }

        public double GetIntegral(double t1, double t2) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetIntegral(t1, t2);
            }
            
            var p = MathHelper.Clamp(P, -1, 1) * 7;
            return Primitive(t2, p) - Primitive(t1, p);
        }

        private static double Primitive(double t, double p) {
            return (Math.Exp(p * t) / p - t) / (Math.Exp(p) - 1);
        }
    }
}
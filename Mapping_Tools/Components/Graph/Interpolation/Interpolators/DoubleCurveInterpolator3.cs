using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Double curve 3")]
    [VerticalMirrorInterpolator]
    [CustomDerivativeExtrema(new []{0, 0.5, 1})]
    public class DoubleCurveInterpolator3 : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
        private readonly LinearInterpolator _linearDegenerate;

        public DoubleCurveInterpolator3() {
            _linearDegenerate = new LinearInterpolator();
            InterpolationFunction = Function;
        }

        public double Function(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetInterpolation(t);
            }

            var p = MathHelper.Clamp(P, -1, 1) * 7;
            return t < 0.5 ? 0.5 * F(t * 2, p) : 0.5 + 0.5 * F(t * 2 - 1, -p);
        }

        private static double F(double t, double k) {
            return (Math.Exp(k) * t) / ((Math.Exp(k) - 1) * t + 1);
        }

        private static double Derivative(double t, double p) {
            return Math.Exp(p) / Math.Pow(t * (Math.Exp(p) - 1) + 1, 2);
        }

        private static double Primitive(double t, double p) {
            return t < 0.5 ? 
                (-(Math.Exp(p) * (Math.Log(2 * t * (Math.Exp(p) - 1) + 1) - 2 * t * (Math.Exp(p) - 1)))) /
                (4 * Math.Pow(Math.Exp(p) - 1, 2)) : 
                (2 * t * (Math.Exp(p) - 2) * (Math.Exp(p) - 1) - 
                 Math.Exp(p) * (Math.Log(-Math.Exp(-p) * (2 * t * Math.Exp(p) - 2 * Math.Exp(p) - 2 * t + 1)) - 
                                Math.Exp(p) - 2) - Math.Exp(2 * p) + Math.Exp(p) * -Math.Log(Math.Exp(p)) - 2) / 
                (4 * Math.Pow(Math.Exp(p) - 1, 2));
        }

        public double GetDerivative(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetDerivative(t);
            }

            var p = MathHelper.Clamp(P, -1, 1) * 7;
            return t < 0.5 ? Derivative(2 * t, p) : Derivative(2 - 2 * t, p);
        }

        public double GetIntegral(double t1, double t2) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetIntegral(t1, t2);
            }

            var p = MathHelper.Clamp(P, -1, 1) * 7;
            return Primitive(t2, p) - Primitive(t1, p);
        }
    }
}
using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Double curve")]
    [VerticalMirrorInterpolator]
    [CustomDerivativeExtrema(new []{0, 0.5, 1})]
    public class DoubleCurveInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
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
            return t < 0.5 ? 0.5 * F(t * 2, p) : 0.5 + 0.5 * F(t * 2 - 1, -p);
        }

        private static double F(double t, double p) {
            return (Math.Exp(p * t) - 1) / (Math.Exp(p) - 1);
        }

        private static double Derivative(double t, double p) {
            return Math.Exp(p * t) * p / (Math.Exp(p) - 1);
        }

        private static double Primitive(double t, double p) {
            return t < 0.5 ? 
                (2 * p * t - Math.Exp(2 * p * t)) / (4 * p - 4 * Math.Exp(p) * p) : 
                (2 * p * ((2 * Math.Exp(p) - 1) * t - Math.Exp(p)) + Math.Exp(p * (2 - 2 * t))) /
                  (4 * (Math.Exp(p) - 1) * p);
        }

        public double GetDerivative(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetDerivative(t);
            }

            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            return t < 0.5 ? Derivative(2 * t, p) : Derivative(2 - 2 * t, p);
        }

        public double GetIntegral(double t1, double t2) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetIntegral(t1, t2);
            }

            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            return Primitive(t2, p) - Primitive(t1, p);
        }
    }
}
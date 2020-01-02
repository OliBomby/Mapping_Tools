using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Half sine")]
    [VerticalMirrorInterpolator]
    public class HalfSineInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
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
            var b = 2 * Math.Acos(1 / (Math.Sqrt(2) * Math.Abs(p) - Math.Abs(p) + 1));
            return p < 0 ?
                1 - F(1 - t, b) :
                F(t, b);
        }

        private static double F(double t, double k) {
            return Math.Sin(t * k) / Math.Sin(k);
        }

        private static double Derivative(double t, double k) {
            return -(2 * k * Math.Sin(k) * Math.Cos(k * t)) / (Math.Cos(2 * k) - 1);
        }

        private static double Primitive(double t, double p) {
            var b = 2 * Math.Acos(1 / (Math.Sqrt(2) * Math.Abs(p) - Math.Abs(p) + 1));
            return p > 0 ?
                (-(MathHelper.Cosec(b) * (Math.Cos(b * t) - 1))) / b :
                MathHelper.Cosec(b) * (Math.Cos(b) - Math.Cos(b - b * t)) / b + t;
        }

        public double GetDerivative(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetDerivative(t);
            }

            var p = MathHelper.Clamp(P, -1, 1);
            var b = 2 * Math.Acos(1 / (Math.Sqrt(2) * Math.Abs(p) - Math.Abs(p) + 1));
            return p < 0 ? 
                Derivative(1 - t, b) : 
                Derivative(t, b);
        }

        public double GetIntegral(double t1, double t2) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetIntegral(t1, t2);
            }

            var p = MathHelper.Clamp(P, -1, 1);
            return Primitive(t2, p) - Primitive(t1, p);
        }
    }
}
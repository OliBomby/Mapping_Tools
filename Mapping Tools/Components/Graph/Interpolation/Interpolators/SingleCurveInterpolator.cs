using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators.Helper_Interpolators;

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

        public IGraphInterpolator GetDerivativeInterpolator() {
            // Can return the same interpolator type, because this interpolator is based on the natural exponent
            return new SingleCurveInterpolator {P = P};
        }

        public double GetDerivative(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetDerivative(t);
            }

            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            return p * Math.Exp(p * t) / (Math.Exp(p) - 1);
        }

        public IGraphInterpolator GetPrimitiveInterpolator(double x1, double y1, double x2, double y2) {
            return new SingleCurveInterpolator {P = P};
        }

        public double GetIntegral(double t1, double t2) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _linearDegenerate.GetIntegral(t1, t2);
            }

            return Primitive(t2) - Primitive(t1);
        }

        private double Primitive(double t) {
            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            return (Math.Exp(p * t) / p - t) / (Math.Exp(p) - 1) - 1 / (p * (Math.Exp(p) - 1));
        }
    }
}
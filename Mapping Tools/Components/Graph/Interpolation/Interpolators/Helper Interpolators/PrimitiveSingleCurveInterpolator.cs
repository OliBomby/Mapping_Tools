using System;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators.Helper_Interpolators {
    [IgnoreInterpolator]
    [VerticalMirrorInterpolator]
    public class PrimitiveSingleCurveInterpolator : CustomInterpolator, IDerivableInterpolator {
        private readonly ParabolaInterpolator _parabolaDegenerate;
        private readonly double _y1;
        private readonly double _y2;

        [UsedImplicitly]
        public PrimitiveSingleCurveInterpolator() : this(0, 1) { }

        public PrimitiveSingleCurveInterpolator(double y1, double y2) {
            _y1 = y1;
            _y2 = y2;
            _parabolaDegenerate = new ParabolaInterpolator {P = (y1 - y2) / (y1 + y2)};
            InterpolationFunction = Function;
        }

        public double Function(double t) {
            if (Math.Abs(P) < Precision.DOUBLE_EPSILON) {
                return _parabolaDegenerate.GetInterpolation(t);
            }

            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            return (-_y1 * Math.Exp(p * t) + _y1 * p * t * Math.Exp(p) + _y1 + _y2 * Math.Exp(p * t) - _y2 * p * t - _y2) /
                   (-_y1 * Math.Exp(p) + _y1 * p * Math.Exp(p) + _y1 + _y2 * Math.Exp(p) - _y2 * p - _y2);
        }

        public IGraphInterpolator GetDerivativeInterpolator() {
            return new SingleCurveInterpolator {P = P};
        }

        public double GetDerivative(double t) {
            var p = -MathHelper.Clamp(P, -1, 1) * 10;
            return (p * ((_y2 - _y1) * Math.Exp(p * t) + _y1 * Math.Exp(p) - _y2)) /
                   (_y1 * Math.Exp(p) * (p - 1) + _y1 + _y2 * (-p + Math.Exp(p) - 1));
        }
    }
}
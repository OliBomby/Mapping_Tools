using System;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators.Helper_Interpolators;

[IgnoreInterpolator]
[VerticalMirrorInterpolator]
public class PrimitiveSingleCurveInterpolator : CustomInterpolator, IDerivableInterpolator {
    private readonly ParabolaInterpolator parabolaDegenerate;
    private readonly double y1;
    private readonly double y2;

    [UsedImplicitly]
    public PrimitiveSingleCurveInterpolator() : this(0, 1) { }

    public PrimitiveSingleCurveInterpolator(double y1, double y2) {
        this.y1 = y1;
        this.y2 = y2;
        parabolaDegenerate = new ParabolaInterpolator {P = (y1 - y2) / (y1 + y2)};
        InterpolationFunction = Function;
    }

    public double Function(double t) {
        if (Math.Abs(P) < Precision.DoubleEpsilon) {
            return parabolaDegenerate.GetInterpolation(t);
        }

        var p = -MathHelper.Clamp(P, -1, 1) * 10;
        return (-y1 * Math.Exp(p * t) + y1 * p * t * Math.Exp(p) + y1 + y2 * Math.Exp(p * t) - y2 * p * t - y2) /
               (-y1 * Math.Exp(p) + y1 * p * Math.Exp(p) + y1 + y2 * Math.Exp(p) - y2 * p - y2);
    }

    public IGraphInterpolator GetDerivativeInterpolator() {
        return new SingleCurveInterpolator {P = P};
    }

    public double GetDerivative(double t) {
        var p = -MathHelper.Clamp(P, -1, 1) * 10;
        return (p * ((y2 - y1) * Math.Exp(p * t) + y1 * Math.Exp(p) - y2)) /
               (y1 * Math.Exp(p) * (p - 1) + y1 + y2 * (-p + Math.Exp(p) - 1));
    }
}
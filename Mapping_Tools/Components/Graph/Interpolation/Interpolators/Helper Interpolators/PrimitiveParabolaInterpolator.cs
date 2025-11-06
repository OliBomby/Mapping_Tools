using System;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators.Helper_Interpolators;

[IgnoreInterpolator]
public class PrimitiveParabolaInterpolator : CustomInterpolator, IDerivableInterpolator {
    private readonly double y1;
    private readonly double y2;

    [UsedImplicitly]
    public PrimitiveParabolaInterpolator() : this(0, 1) { }

    public PrimitiveParabolaInterpolator(double y1, double y2) {
        this.y1 = y1;
        this.y2 = y2;
        InterpolationFunction = Function;
    }

    public double Function(double t) {
        var p = MathHelper.Clamp(P, -1, 1);
        return (t * (2 * p * Math.Pow(t, 2) * (y2 - y1) + 3 * (p + 1) * t * (y1 - y2) - 6 * y1)) / (y1 * (p - 3) - y2 * (p + 3));
    }

    public double GetDerivative(double t) {
        var p = MathHelper.Clamp(P, -1, 1);
        return -(6 * (y1 * (t - 1) * (p * t - 1) + y2 * t * (p * -t + p + 1))) / (y1 * (p - 3) - y2 * (p + 3));
    }
}
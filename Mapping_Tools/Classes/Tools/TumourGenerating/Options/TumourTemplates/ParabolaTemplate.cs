using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates;

public class ParabolaTemplate : TumourTemplateBase {
    public override Vector2 GetOffset(double t) {
        return (4 * t * t - 4 * t) * Width * Vector2.UnitY;
    }

    private static double CalculateIntegral(double t, double a, double b) {
        var x = b * (1 - 2 * t);
        return -(4 * a * x * Math.Sqrt(16 * Math.Pow(a, 2) * Math.Pow(x, 2) + Math.Pow(b, 4)) + Math.Pow(b, 4) *
            MathHelper.HArcsin((4 * a * x) / Math.Pow(b, 2)))/(16 * a * Math.Pow(b, 2));
    }

    private static double CalculateLength(double t, double a, double b) {
        return CalculateIntegral(t, a, b) - CalculateIntegral(0, a, b);
    }

    public override double GetLength() {
        return CalculateLength(1, Width, Length);
    }

    public override double GetDefaultSpan() {
        return Length;
    }

    public override int GetDetailLevel() {
        return 10;
    }

    public override IEnumerable<double> GetCriticalPoints() {
        return Enumerable.Empty<double>();
    }

    public override List<Vector2> GetReconstructionHint() {
        return new List<Vector2> { Vector2.Zero, new(0.5 * Length, -2 * Width), Length * Vector2.UnitX };
    }

    public override PathType GetReconstructionHintPathType() {
        return PathType.Bezier;
    }

    public override Func<double, double> GetDistanceRelation() {
        var totalLength = GetLength();
        var width = Width;
        var length = Length;
        return t => CalculateLength(t, width, length) / totalLength;
    }
}
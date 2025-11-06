using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates;

public class CircleTemplate : TumourTemplateBase, IRequireInit {
    private double centreY;
    private double radius;
    private double theta;
    private int dir;
    private bool stable;

    private double ThetaRange => dir == 0 ? 0 : Math.PI + 2 * dir * theta;

    public override Vector2 GetOffset(double t) {
        if (!stable) {
            return (4 * t * t - 4 * t) * Width * Vector2.UnitY;
        }

        var a = Math.PI * dir * (t - 1) + (t * 2 - 1) * theta;
        return new Vector2(Math.Cos(a) * radius - (t - 0.5) * Length, Math.Sin(a) * radius + centreY);
    }

    public override double GetLength() {
        return radius * ThetaRange;
    }

    public override double GetDefaultSpan() {
        return Length;
    }

    public override int GetDetailLevel() {
        return (int) Math.Ceiling(10 * ThetaRange / Math.PI);
    }

    public override IEnumerable<double> GetCriticalPoints() {
        return Enumerable.Empty<double>();
    }

    public override List<Vector2> GetReconstructionHint() {
        if (Precision.AlmostEquals(Length, 0, 1E-3D)) return null;
        return new List<Vector2> { Vector2.Zero, new(0.5 * Length, -Width), Length * Vector2.UnitX };
    }

    public override PathType GetReconstructionHintPathType() {
        return PathType.PerfectCurve;
    }

    public override Func<double, double> GetDistanceRelation() {
        return null;
    }

    public void Init() {
        // If we have a degenerate triangle where the size is almost zero, then give up and fall
        // back to a more numerically stable method.
        if (Precision.AlmostEquals(Width, 0, 1E-5D)) {
            centreY = 0;
            radius = 0;
            theta = 0;
            dir = 0;
            stable = false;
            return;
        }

        // If we have a full circle, then its easy
        if (Precision.AlmostEquals(Length, 0, 1E-5D)) {
            centreY = -0.5 * Width;
            radius = 0.5 * Math.Abs(Width);
            dir = Math.Sign(Width);
            theta = 0.5 * dir * Math.PI;
            stable = true;
            return;
        }

        double aSq = 0.25 * Length * Length + Width * Width;
        double bSq = Length * Length;

        double s = aSq * bSq;
        double t = bSq * (2 * aSq - bSq);
        double sum = 2 * s + t;

        centreY = t * -Width / sum;
        radius = Math.Sqrt(0.25 * Length * Length + centreY * centreY);
        theta = -Math.Atan2(centreY, 0.5 * Length);
        dir = Math.Sign(Width);
        stable = true;
    }
}
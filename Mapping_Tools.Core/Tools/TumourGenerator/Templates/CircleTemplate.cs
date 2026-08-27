using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.TumourGenerator.Templates;

internal sealed class CircleTemplate : TumourTemplateBase, IRequireInit
{
    private double centreY;
    private int direction;
    private double radius;
    private bool stable;
    private double theta;
    private double ThetaRange => direction == 0 ? 0 : Math.PI + 2 * direction * theta;

    public void Init()
    {
        if (Precision.AlmostEquals(Width, 0, 1E-5D))
        {
            centreY = radius = theta = direction = 0;
            stable = false;
            return;
        }

        if (Precision.AlmostEquals(Length, 0, 1E-5D))
        {
            centreY = -0.5 * Width;
            radius = 0.5 * Math.Abs(Width);
            direction = Math.Sign(Width);
            theta = 0.5 * direction * Math.PI;
            stable = true;
            return;
        }

        double aSquared = 0.25 * Length * Length + Width * Width;
        double bSquared = Length * Length;
        double product = aSquared * bSquared;
        double second = bSquared * (2 * aSquared - bSquared);
        double sum = 2 * product + second;
        centreY = second * -Width / sum;
        radius = Math.Sqrt(0.25 * Length * Length + centreY * centreY);
        theta = -Math.Atan2(centreY, 0.5 * Length);
        direction = Math.Sign(Width);
        stable = true;
    }

    public override Vector2 GetOffset(double t)
    {
        if (!stable) return (4 * t * t - 4 * t) * Width * Vector2.UnitY;
        double angle = Math.PI * direction * (t - 1) + (t * 2 - 1) * theta;
        return new Vector2(Math.Cos(angle) * radius - (t - 0.5) * Length, Math.Sin(angle) * radius + centreY);
    }

    public override double GetLength()
    {
        return radius * ThetaRange;
    }

    public override double GetDefaultSpan()
    {
        return Length;
    }

    public override int GetDetailLevel()
    {
        return (int)Math.Ceiling(10 * ThetaRange / Math.PI);
    }

    public override IEnumerable<double> GetCriticalPoints()
    {
        return [];
    }

    public override List<Vector2>? GetReconstructionHint()
    {
        return Precision.AlmostEquals(Length, 0, 1E-3D)
            ? null
            : [Vector2.Zero, new Vector2(0.5 * Length, -Width), Length * Vector2.UnitX];
    }

    public override PathType GetReconstructionHintPathType()
    {
        return PathType.PerfectCurve;
    }

    public override Func<double, double>? GetDistanceRelation()
    {
        return null;
    }
}


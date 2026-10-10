using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.TumourGenerator.Templates;

internal sealed class TriangleTemplate : TumourTemplateBase
{
    public override Vector2 GetOffset(double t)
    {
        return t < 0.5
            ? -2 * Width * t * Vector2.UnitY
            : 2 * Width * (-1 + t) * Vector2.UnitY;
    }

    public override double GetLength()
    {
        return 2 * Math.Sqrt(0.25 * Length * Length + Width * Width);
    }

    public override double GetDefaultSpan()
    {
        return Length;
    }

    public override int GetDetailLevel()
    {
        return 1;
    }

    public override IEnumerable<double> GetCriticalPoints() { yield return 0.5; }

    public override List<PathControlPoint> GetReconstructionHint()
    {
        return
        [
            new PathControlPoint(Vector2.Zero, PathType.Linear),
            new PathControlPoint(new Vector2(0.5 * Length, -Width)),
            new PathControlPoint(Length * Vector2.UnitX),
        ];
    }

    public override Func<double, double>? GetDistanceRelation()
    {
        return null;
    }
}

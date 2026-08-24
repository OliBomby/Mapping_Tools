using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.TumourGenerating.Templates;

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

    public override List<Vector2> GetReconstructionHint()
    {
        return [Vector2.Zero, new Vector2(0.5 * Length, -Width), Length * Vector2.UnitX];
    }

    public override PathType GetReconstructionHintPathType()
    {
        return PathType.Linear;
    }

    public override Func<double, double>? GetDistanceRelation()
    {
        return null;
    }
}


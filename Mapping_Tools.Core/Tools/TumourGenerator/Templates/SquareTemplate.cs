using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.TumourGenerator.Templates;

internal sealed class SquareTemplate : TumourTemplateBase, IRequireInit
{
    private const double min_side_margin = 0.0001;
    private double sideMargin;
    public override bool NeedsParameter => true;

    public void Init()
    {
        sideMargin = Precision.AlmostEquals(Length, 0)
            ? min_side_margin
            : MathHelper.Clamp(Parameter / Length, min_side_margin, 0.5);
    }

    public override Vector2 GetOffset(double t)
    {
        return t < sideMargin
            ? -t * Width / sideMargin * Vector2.UnitY
            : t > 1 - sideMargin
                ? (t - 1) * Width / sideMargin * Vector2.UnitY
                : -Width * Vector2.UnitY;
    }

    public override double GetLength()
    {
        double marginLength = Math.Sqrt(Width * Width + Length * Length * sideMargin * sideMargin);
        return 2 * marginLength + Length * (1 - 2 * sideMargin);
    }

    public override double GetDefaultSpan()
    {
        return Length;
    }

    public override int GetDetailLevel()
    {
        return 1;
    }

    public override IEnumerable<double> GetCriticalPoints()
    {
        yield return sideMargin;
        yield return 1 - sideMargin;
    }

    public override List<Vector2> GetReconstructionHint()
    {
        return [Vector2.Zero, new Vector2(sideMargin * Length, -Width), new Vector2((1 - sideMargin) * Length, -Width), Length * Vector2.UnitX];
    }

    public override PathType GetReconstructionHintPathType()
    {
        return PathType.Linear;
    }

    public override Func<double, double>? GetDistanceRelation()
    {
        double length = Length;
        double width = Width;
        double margin = sideMargin;
        return t => DistanceRelation(t, length, width, margin);
    }

    private static double DistanceRelation(double t, double scaleX, double scaleY, double margin)
    {
        double marginLength = Math.Sqrt(scaleY * scaleY + scaleX * scaleX * margin * margin);
        double length = 2 * marginLength + scaleX * (1 - 2 * margin);
        return t < margin ? t / margin * marginLength / length
            : t > 1 - margin ? 1 + (t - 1) / margin * marginLength / length
            : (t - margin) * scaleX / length + marginLength / length;
    }
}


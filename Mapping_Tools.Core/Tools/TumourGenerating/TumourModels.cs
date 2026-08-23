using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Classes.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.Classes.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.TumourGenerating;

/// <summary>Chooses the side of the slider on which a tumour is placed.</summary>
public enum TumourSidedness
{
    /// <summary>Places every tumour on the left side.</summary>
    Left,

    /// <summary>Places every tumour on the right side.</summary>
    Right,

    /// <summary>Alternates tumours, beginning on the left side.</summary>
    AlternatingLeft,

    /// <summary>Alternates tumours, beginning on the right side.</summary>
    AlternatingRight,

    /// <summary>Chooses each tumour side from the layer's random sequence.</summary>
    Random,
}

/// <summary>Identifies the geometric template used for one tumour.</summary>
public enum TumourTemplate
{
    /// <summary>A triangular protrusion.</summary>
    Triangle,

    /// <summary>A square-topped protrusion.</summary>
    Square,

    /// <summary>A circular-arc protrusion.</summary>
    Circle,

    /// <summary>A parabolic protrusion.</summary>
    Parabola,
}

/// <summary>Controls whether a tumour follows the original or wrapped slider path.</summary>
public enum WrappingMode
{
    /// <summary>Uses the straight direction between the tumour endpoints.</summary>
    Simple,

    /// <summary>Follows the local direction of the existing slider path.</summary>
    Wrap,

    /// <summary>Uses the path point's original angle without an additional offset.</summary>
    Absolute,
}

/// <summary>Provides the shape contract consumed by the tumour path algorithm.</summary>
public interface ITumourTemplate
{
    /// <summary>Gets or sets the longitudinal length in osu! pixels.</summary>
    double Length { get; set; }

    /// <summary>Gets or sets the signed protrusion width in osu! pixels.</summary>
    double Width { get; set; }

    /// <summary>Gets or sets the optional shape parameter.</summary>
    double Parameter { get; set; }

    /// <summary>Gets whether <see cref="Parameter" /> changes this template.</summary>
    bool NeedsParameter { get; }

    /// <summary>Gets the offset at normalized template progress.</summary>
    /// <param name="t">The template progress from zero to one.</param>
    /// <returns>The offset from the underlying slider path.</returns>
    Vector2 GetOffset(double t);

    /// <summary>Gets the approximated curve length of the configured template.</summary>
    double GetLength();

    /// <summary>Gets the default endpoint span for this template.</summary>
    double GetDefaultSpan();

    /// <summary>Gets the minimum detail level used for path approximation.</summary>
    int GetDetailLevel();

    /// <summary>Gets normalized points that must be retained as path points.</summary>
    IEnumerable<double> GetCriticalPoints();

    /// <summary>Gets reconstruction anchors in the template's local coordinates.</summary>
    List<Vector2>? GetReconstructionHint();

    /// <summary>Gets the path type for the reconstruction hint.</summary>
    PathType GetReconstructionHintPathType();

    /// <summary>Gets the cumulative-distance relation for a reconstruction hint.</summary>
    Func<double, double>? GetDistanceRelation();
}

/// <summary>Marks templates that require initialization after their dimensions are set.</summary>
public interface IRequireInit
{
    /// <summary>Recomputes any cached shape values from the current template properties.</summary>
    void Init();
}

/// <summary>Describes one configurable tumour layer without UI dependencies.</summary>
public sealed class TumourLayer
{
    /// <summary>Gets or sets the selected template.</summary>
    public TumourTemplate TumourTemplateEnum { get; set; }

    /// <summary>Gets a fresh configured template instance for this layer.</summary>
    [JsonIgnore]
    public ITumourTemplate TumourTemplate => TumourTemplateEnum switch
    {
        TumourGenerating.TumourTemplate.Triangle => new TriangleTemplate(),
        TumourGenerating.TumourTemplate.Square => new SquareTemplate(),
        TumourGenerating.TumourTemplate.Circle => new CircleTemplate(),
        TumourGenerating.TumourTemplate.Parabola => new ParabolaTemplate(),
        _ => new TriangleTemplate(),
    };

    /// <summary>Gets or sets how the tumour follows the slider path.</summary>
    public WrappingMode WrappingMode { get; set; }

    /// <summary>Gets or sets the side-selection policy.</summary>
    public TumourSidedness TumourSidedness { get; set; }

    /// <summary>Gets or sets the graph controlling tumour length.</summary>
    public GraphState TumourLength { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the graph controlling tumour scale.</summary>
    public GraphState TumourScale { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the graph controlling tumour rotation in degrees.</summary>
    public GraphState TumourRotation { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the graph controlling the template parameter.</summary>
    public GraphState TumourParameter { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the graph controlling spacing between tumours.</summary>
    public GraphState TumourDistance { get; set; } = GraphState.CreateDefault();

    /// <summary>Gets or sets the explicit tumour count, or zero for distance-based count.</summary>
    public int TumourCount { get; set; }

    /// <summary>Gets or sets the sequence start in relative or absolute units.</summary>
    public double TumourStart { get; set; }

    /// <summary>Gets or sets the sequence end in relative or absolute units.</summary>
    public double TumourEnd { get; set; }

    /// <summary>Gets or sets the deterministic random-side seed; zero uses the generator sequence.</summary>
    public int RandomSeed { get; set; }

    /// <summary>Gets or sets whether this layer recalculates the path before placement.</summary>
    public bool Recalculate { get; set; }

    /// <summary>Gets or sets whether range and shape values are absolute osu! pixels.</summary>
    public bool UseAbsoluteRange { get; set; }

    /// <summary>Gets or sets whether this layer participates in generation.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the user-facing layer name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Creates the legacy default layer, including constant graph values.</summary>
    /// <returns>A new active triangle layer.</returns>
    public static TumourLayer GetDefaultLayer()
    {
        return new TumourLayer
        {
            TumourTemplateEnum = TumourGenerating.TumourTemplate.Triangle,
            WrappingMode = WrappingMode.Simple,
            TumourSidedness = TumourSidedness.Left,
            IsActive = true,
            Name = "Layer",
            TumourCount = 0,
            TumourStart = 0,
            TumourEnd = 256,
            TumourLength = GetGraphState(15),
            TumourScale = GetGraphState(30),
            TumourRotation = GetGraphState(0),
            TumourParameter = GetGraphState(0),
            TumourDistance = GetGraphState(100),
            RandomSeed = 0,
            UseAbsoluteRange = true,
            Recalculate = true,
        };
    }

    /// <summary>Creates a constant graph with bounds matching the legacy editor.</summary>
    /// <param name="value">The constant graph value.</param>
    /// <returns>A graph with two single-curve edge anchors.</returns>
    public static GraphState GetGraphState(double value)
    {
        return new GraphState(
            [
                new GraphAnchor(new Vector2(0, (float)value), new SingleCurveInterpolator()),
                new GraphAnchor(new Vector2(1, (float)value), new SingleCurveInterpolator()),
            ],
            0,
            Math.Min(0, value * 2),
            1,
            Math.Max(0, value * 2));
    }

    /// <summary>Creates a deep copy suitable for editing independently.</summary>
    /// <returns>A mutable copy of this layer and all graph state.</returns>
    public TumourLayer Copy()
    {
        return new TumourLayer
        {
            TumourTemplateEnum = TumourTemplateEnum,
            WrappingMode = WrappingMode,
            TumourSidedness = TumourSidedness,
            TumourLength = TumourLength.Clone(),
            TumourScale = TumourScale.Clone(),
            TumourRotation = TumourRotation.Clone(),
            TumourParameter = TumourParameter.Clone(),
            TumourDistance = TumourDistance.Clone(),
            TumourCount = TumourCount,
            TumourStart = TumourStart,
            TumourEnd = TumourEnd,
            RandomSeed = RandomSeed,
            Recalculate = Recalculate,
            UseAbsoluteRange = UseAbsoluteRange,
            IsActive = IsActive,
            Name = Name,
        };
    }
}

/// <summary>Groups the framework-neutral settings used by the tumour generator.</summary>
public class TumourGeneratorOptions
{
    /// <summary>Gets or sets the configured ordered tumour layers.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<TumourLayer> TumourLayers { get; set; } = [];

    /// <summary>Gets or sets whether only middle anchors are retained.</summary>
    public bool JustMiddleAnchors { get; set; }

    /// <summary>Gets or sets the global tumour size scalar.</summary>
    public double Scale { get; set; } = 1;

    /// <summary>Gets or sets whether intelligent path reconstruction is skipped.</summary>
    public bool DebugConstruction { get; set; }

    /// <summary>Gets or sets whether slider velocity is corrected after generation.</summary>
    public bool FixSv { get; set; } = true;

    /// <summary>Gets or sets whether corrected velocity is delegated to BPM redlines.</summary>
    public bool DelegateToBpm { get; set; }

    /// <summary>Gets or sets whether delegated velocity removes slider ticks.</summary>
    public bool RemoveSliderTicks { get; set; }
}

/// <summary>Describes the scalar values supplied to a generated tumour.</summary>
public interface ITumourAssignment
{
    /// <summary>Gets the tumour start distance in pixels.</summary>
    double Start { get; }

    /// <summary>Gets the tumour end distance in pixels.</summary>
    double End { get; }

    /// <summary>Gets the longitudinal tumour length.</summary>
    double Length { get; }

    /// <summary>Gets the signed tumour scale.</summary>
    double Scalar { get; }

    /// <summary>Gets the tumour rotation in radians.</summary>
    double Rotation { get; }

    /// <summary>Gets the path wrapping mode.</summary>
    WrappingMode WrappingMode { get; }

    /// <summary>Gets whether the tumour is placed on the opposite side.</summary>
    bool Inverted { get; }

    /// <summary>Gets the configured tumour shape.</summary>
    ITumourTemplate GetTemplate();
}

internal abstract class TumourTemplateBase : ITumourTemplate
{
    [JsonIgnore] public double Length { get; set; }
    [JsonIgnore] public double Width { get; set; }
    [JsonIgnore] public double Parameter { get; set; }
    public virtual bool NeedsParameter => false;
    public abstract Vector2 GetOffset(double t);
    public abstract double GetLength();
    public abstract double GetDefaultSpan();
    public abstract int GetDetailLevel();
    public abstract IEnumerable<double> GetCriticalPoints();
    public abstract List<Vector2>? GetReconstructionHint();
    public abstract PathType GetReconstructionHintPathType();
    public abstract Func<double, double>? GetDistanceRelation();
}

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

internal sealed class SquareTemplate : TumourTemplateBase, IRequireInit
{
    private const double MinSideMargin = 0.0001;
    private double sideMargin;
    public override bool NeedsParameter => true;

    public void Init()
    {
        sideMargin = Precision.AlmostEquals(Length, 0)
            ? MinSideMargin
            : MathHelper.Clamp(Parameter / Length, MinSideMargin, 0.5);
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

internal sealed class ParabolaTemplate : TumourTemplateBase
{
    public override Vector2 GetOffset(double t)
    {
        return (4 * t * t - 4 * t) * Width * Vector2.UnitY;
    }

    public override double GetLength()
    {
        return CalculateLength(1, Width, Length);
    }

    public override double GetDefaultSpan()
    {
        return Length;
    }

    public override int GetDetailLevel()
    {
        return 10;
    }

    public override IEnumerable<double> GetCriticalPoints()
    {
        return [];
    }

    public override List<Vector2> GetReconstructionHint()
    {
        return [Vector2.Zero, new Vector2(0.5 * Length, -2 * Width), Length * Vector2.UnitX];
    }

    public override PathType GetReconstructionHintPathType()
    {
        return PathType.Bezier;
    }

    public override Func<double, double>? GetDistanceRelation()
    {
        double totalLength = GetLength();
        double width = Width;
        double length = Length;
        return t => CalculateLength(t, width, length) / totalLength;
    }

    private static double CalculateIntegral(double t, double a, double b)
    {
        double x = b * (1 - 2 * t);
        return -(4 * a * x * Math.Sqrt(16 * Math.Pow(a, 2) * Math.Pow(x, 2) + Math.Pow(b, 4)) + Math.Pow(b, 4) * MathHelper.HArcsin(4 * a * x / Math.Pow(b, 2)))
               / (16 * a * Math.Pow(b, 2));
    }

    private static double CalculateLength(double t, double a, double b)
    {
        return CalculateIntegral(t, a, b) - CalculateIntegral(0, a, b);
    }
}

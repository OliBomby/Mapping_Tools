using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;

/// <summary>Represents an infinite line generated in editor coordinates.</summary>
public sealed class RelevantLine : RelevantDrawable
{
    /// <summary>Creates a line for deserialization.</summary>
    public RelevantLine() { }

    /// <summary>Creates a line from the supplied geometry.</summary>
    /// <param name="line">The infinite line.</param>
    public RelevantLine(Line2 line)
    {
        Child = line;
    }

    /// <summary>Gets the stable preference-group name for lines.</summary>
    public static string PreferencesNameStatic => "Virtual line preferences";

    /// <inheritdoc />
    public override string PreferencesName => PreferencesNameStatic;

    /// <summary>Gets or sets the infinite line geometry.</summary>
    public Line2 Child { get; set; }

    /// <inheritdoc />
    public override double DistanceTo(Vector2 point)
    {
        return Line2.Distance(Child, point);
    }

    /// <inheritdoc />
    public override bool Intersection(IRelevantObject other, out Vector2[] intersections)
    {
        switch (other)
        {
            case RelevantPoint point:
                intersections = new[] { point.Child };
                return Precision.AlmostEquals(Line2.Distance(Child, point.Child), 0);
            case RelevantLine line:
                bool isIntersecting = Line2.Intersection(Child, line.Child, out var intersection);
                intersections = new[] { intersection };
                return isIntersecting;
            case RelevantCircle circle:
                return Circle.Intersection(circle.Child, Child, out intersections);
            default:
                intersections = [];
                return false;
        }
    }

    /// <inheritdoc />
    public override Vector2 NearestPoint(Vector2 point)
    {
        return Line2.NearestPoint(Child, point);
    }

    /// <inheritdoc />
    public override double DistanceTo(IRelevantObject relevantObject)
    {
        if (relevantObject is not RelevantLine line) return double.PositiveInfinity;

        double cosAlpha = Vector2.Dot(Child.DirectionVector, line.Child.DirectionVector) / (Child.DirectionVector.Length * line.Child.DirectionVector.Length);
        double angleDiff = Math.Sqrt(10000 / (cosAlpha * cosAlpha) - 10000);
        return Line2.Distance(Child, line.Child.PositionVector) + angleDiff;
    }
}


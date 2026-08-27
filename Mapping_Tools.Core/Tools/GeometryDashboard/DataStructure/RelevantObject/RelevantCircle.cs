using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;

/// <summary>Represents a circle generated in editor coordinates.</summary>
public sealed class RelevantCircle : RelevantDrawable
{
    /// <summary>Creates a circle for deserialization.</summary>
    public RelevantCircle() { }

    /// <summary>Creates a circle from the supplied geometry.</summary>
    /// <param name="circle">The circle geometry.</param>
    public RelevantCircle(Circle circle)
    {
        Child = circle;
    }

    /// <summary>Gets the stable preference-group name for circles.</summary>
    public static string PreferencesNameStatic => "Virtual circle preferences";

    /// <inheritdoc />
    public override string PreferencesName => PreferencesNameStatic;

    /// <summary>Gets or sets the circle geometry.</summary>
    public Circle Child { get; set; }

    /// <inheritdoc />
    public override double DistanceTo(Vector2 point)
    {
        return Math.Abs(Vector2.Distance(point, Child.Centre) - Child.Radius);
    }

    /// <inheritdoc />
    public override bool Intersection(IRelevantObject other, out Vector2[] intersections)
    {
        switch (other)
        {
            case RelevantPoint point:
                intersections = new[] { point.Child };
                return Precision.AlmostEquals(Vector2.Distance(Child.Centre, point.Child), Child.Radius);
            case RelevantLine line:
                return Circle.Intersection(Child, line.Child, out intersections);
            case RelevantCircle circle:
                return Circle.Intersection(Child, circle.Child, out intersections);
            default:
                intersections = [];
                return false;
        }
    }

    /// <inheritdoc />
    public override Vector2 NearestPoint(Vector2 point)
    {
        var diff = point - Child.Centre;
        double dist = diff.Length;
        if (Precision.AlmostEquals(dist, 0)) return Child.Centre + new Vector2(Child.Radius, 0);

        return Child.Centre + diff / dist * Child.Radius;
    }

    /// <inheritdoc />
    public override double DistanceTo(IRelevantObject relevantObject)
    {
        return relevantObject is RelevantCircle circle
            ? Vector2.Distance(Child.Centre, circle.Child.Centre) + Math.Abs(Child.Radius - circle.Child.Radius)
            : double.PositiveInfinity;
    }
}


using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;

/// <summary>Represents a point generated in editor coordinates.</summary>
public sealed class RelevantPoint : RelevantDrawable
{
    /// <summary>Creates a point for deserialization.</summary>
    public RelevantPoint() { }

    /// <summary>Creates a point at the supplied editor coordinate.</summary>
    /// <param name="vec">The editor-space coordinate.</param>
    public RelevantPoint(Vector2 vec)
    {
        Child = vec;
    }

    /// <summary>Gets the stable preference-group name for points.</summary>
    public static string PreferencesNameStatic => "Virtual point preferences";

    /// <inheritdoc />
    public override string PreferencesName => PreferencesNameStatic;

    /// <summary>Gets or sets the editor-space point.</summary>
    public Vector2 Child { get; set; }

    /// <inheritdoc />
    public override double DistanceTo(Vector2 point)
    {
        return Vector2.Distance(Child, point);
    }

    /// <inheritdoc />
    public override bool Intersection(IRelevantObject other, out Vector2[] intersections)
    {
        intersections = new[] { Child };
        return other switch
        {
            RelevantPoint point => Precision.AlmostEquals(point.Child.X, Child.X) & Precision.AlmostEquals(point.Child.Y, Child.Y),
            RelevantLine line => Precision.AlmostEquals(Line2.Distance(line.Child, Child), 0),
            RelevantCircle circle => Precision.AlmostEquals(Vector2.Distance(circle.Child.Centre, Child), circle.Child.Radius),
            _ => false,
        };
    }

    /// <inheritdoc />
    public override Vector2 NearestPoint(Vector2 point)
    {
        return Child;
    }

    /// <inheritdoc />
    public override double DistanceTo(IRelevantObject relevantObject)
    {
        return relevantObject is RelevantPoint point
            ? Vector2.Distance(Child, point.Child)
            : double.PositiveInfinity;
    }
}


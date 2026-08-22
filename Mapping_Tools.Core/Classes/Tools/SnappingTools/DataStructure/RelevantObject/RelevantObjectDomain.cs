using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject
{

/// <summary>Describes a generated object that can participate in geometric hit testing.</summary>
public interface IRelevantDrawable : IRelevantObject
{
    /// <summary>Gets the preference-group name used by the later overlay renderer.</summary>
    string PreferencesName { get; }

    /// <summary>Measures the distance from this geometry to an editor-space point.</summary>
    /// <param name="point">The editor-space point.</param>
    /// <returns>The shortest distance in editor pixels.</returns>
    double DistanceTo(Vector2 point);

    /// <summary>Finds the point on this geometry nearest to an editor-space point.</summary>
    /// <param name="point">The editor-space point.</param>
    /// <returns>The nearest point in editor coordinates.</returns>
    Vector2 NearestPoint(Vector2 point);

    /// <summary>Finds intersections with another compatible generated object.</summary>
    /// <param name="other">The other generated object.</param>
    /// <param name="intersections">The intersection points, including a value when the query fails.</param>
    /// <returns><see langword="true"/> when at least one intersection exists.</returns>
    bool Intersection(IRelevantObject other, out Vector2[] intersections);
}

/// <summary>Owns the mutable graph state shared by all relevant-object shapes.</summary>
public interface IRelevantObject : IDisposable
{
    /// <summary>Gets or sets the derived timestamp used for layer ordering.</summary>
    double Time { get; set; }

    /// <summary>Gets or sets the relevance weight, normally between zero and one.</summary>
    double Relevancy { get; set; }

    /// <summary>Gets or sets whether this object has been removed from the graph.</summary>
    bool Disposed { get; set; }

    /// <summary>Gets or sets the regeneration marker that protects this object temporarily.</summary>
    bool DoNotDispose { get; set; }

    /// <summary>Gets or sets the regeneration marker requesting removal.</summary>
    bool DefinitelyDispose { get; set; }

    /// <summary>Gets or sets whether changes automatically regenerate descendants.</summary>
    bool AutoPropagate { get; set; }

    /// <summary>Gets or sets whether this object is selected for generator and UI rules.</summary>
    bool IsSelected { get; set; }

    /// <summary>Gets or sets whether this object is detached and protected from inheritance.</summary>
    bool IsLocked { get; set; }

    /// <summary>Gets or sets whether generators may use this object as an input.</summary>
    bool IsInheritable { get; set; }

    /// <summary>Gets or sets the owning layer; this relationship is runtime-only.</summary>
    [JsonIgnore]
    RelevantObjectLayer? Layer { get; set; }

    /// <summary>Gets or sets the generator that produced this object; runtime-only.</summary>
    [JsonIgnore]
    RelevantObjectsGenerator? Generator { get; set; }

    /// <summary>Gets or sets the source objects that produced this object; runtime-only.</summary>
    [JsonIgnore]
    HashSet<IRelevantObject> ParentObjects { get; set; }

    /// <summary>Gets or sets the generated descendants of this object; runtime-only.</summary>
    [JsonIgnore]
    HashSet<IRelevantObject> ChildObjects { get; set; }

    /// <summary>Gets this object and its ancestors up to the requested depth.</summary>
    /// <param name="level">The maximum number of parent links to traverse.</param>
    /// <returns>A set containing this object and the discovered ancestors.</returns>
    HashSet<IRelevantObject> GetParentage(int level);

    /// <summary>Gets this object and its descendants up to the requested depth.</summary>
    /// <param name="level">The maximum number of child links to traverse.</param>
    /// <returns>A set containing this object and the discovered descendants.</returns>
    HashSet<IRelevantObject> GetDescendants(int level);

    /// <summary>Recomputes this object's relevance from its current parents.</summary>
    void UpdateRelevancy();

    /// <summary>Recomputes this object's timestamp from its generator positioning rule.</summary>
    void UpdateTime();

    /// <summary>Creates a detached locked copy for persistence or manual save slots.</summary>
    /// <returns>A locked object with no runtime graph links.</returns>
    IRelevantObject GetLockedRelevantObject();

    /// <summary>Merges another equivalent object into this object's graph state.</summary>
    /// <param name="other">The object whose parents and descendants are consumed.</param>
    void Consume(IRelevantObject other);

    /// <summary>Measures graph-specific similarity to another object.</summary>
    /// <param name="relevantObject">The object to compare with this object.</param>
    /// <returns>A distance value in the object's native geometry units.</returns>
    double DistanceTo(IRelevantObject relevantObject);
}

/// <summary>Base implementation for generated objects and their ownership graph.</summary>
public abstract class RelevantObject : IRelevantObject
{
    private double _time;
    private double _customTime;
    private double _relevancy;
    private bool _isSelected;
    private bool _isLocked;
    private bool _isInheritable = true;
    private HashSet<IRelevantObject> _parentObjects = [];
    private HashSet<IRelevantObject> _childObjects = [];

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Disposed)
        {
            return;
        }

        Layer?.Remove(this, false);
        Disposed = true;

        if (ParentObjects is not null)
        {
            foreach (IRelevantObject relevantObject in ParentObjects)
            {
                relevantObject.ChildObjects.Remove(this);
            }
        }

        if (ChildObjects is null)
        {
            return;
        }

        IRelevantObject[] objectsToDispose = ChildObjects.ToArray();
        foreach (IRelevantObject child in objectsToDispose)
        {
            child.Dispose();
        }
    }

    /// <inheritdoc/>
    public virtual double Time
    {
        get => _time;
        set
        {
            _time = value;
            if (ChildObjects is null)
            {
                return;
            }

            foreach (IRelevantObject relevantObject in ChildObjects)
            {
                relevantObject.UpdateTime();
            }

            Layer?.SortTimes();
        }
    }

    /// <summary>Gets or sets the manually supplied time used by custom positioning generators.</summary>
    public double CustomTime
    {
        get => _customTime;
        set
        {
            _customTime = value;
            if (Generator?.TemporalPositioning != GeneratorTemporalPositioning.Custom)
            {
                return;
            }

            UpdateTime();
        }
    }

    /// <inheritdoc/>
    public double Relevancy
    {
        get => _isSelected ? 1 : _relevancy;
        set
        {
            _relevancy = value;
            if (ChildObjects is null)
            {
                return;
            }

            foreach (IRelevantObject relevantObject in ChildObjects)
            {
                relevantObject.UpdateRelevancy();
            }
        }
    }

    /// <inheritdoc/>
    public bool Disposed { get; set; }

    /// <inheritdoc/>
    public bool DoNotDispose { get; set; }

    /// <inheritdoc/>
    public bool DefinitelyDispose { get; set; }

    /// <inheritdoc/>
    public bool AutoPropagate { get; set; } = true;

    /// <inheritdoc/>
    public virtual bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            if (ChildObjects is not null)
            {
                foreach (IRelevantObject relevantObject in ChildObjects)
                {
                    relevantObject.UpdateRelevancy();
                }
            }

            if (AutoPropagate)
            {
                Layer?.NextLayer?.GenerateNewObjects(true);
            }
        }
    }

    /// <inheritdoc/>
    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked == value)
            {
                return;
            }

            _isLocked = value;
            if (AutoPropagate)
            {
                Layer?.NextLayer?.GenerateNewObjects(true);
            }
        }
    }

    /// <inheritdoc/>
    public bool IsInheritable
    {
        get => _isInheritable;
        set
        {
            if (_isInheritable == value)
            {
                return;
            }

            _isInheritable = value;
            if (!AutoPropagate)
            {
                return;
            }

            if (_isInheritable)
            {
                Layer?.NextLayer?.GenerateNewObjects(true);
            }
            else
            {
                IRelevantObject[] objectsToDispose = ChildObjects.ToArray();
                foreach (IRelevantObject child in objectsToDispose)
                {
                    child.Dispose();
                }

                Layer?.NextLayer?.GenerateNewObjects(true);
            }
        }
    }

    /// <inheritdoc/>
    public RelevantObjectLayer? Layer { get; set; }

    /// <inheritdoc/>
    public RelevantObjectsGenerator? Generator { get; set; }

    /// <inheritdoc/>
    public HashSet<IRelevantObject> ParentObjects
    {
        get => _parentObjects;
        set
        {
            _parentObjects = value ?? [];
            UpdateRelevancy();
            UpdateTime();
        }
    }

    /// <inheritdoc/>
    public HashSet<IRelevantObject> ChildObjects
    {
        get => _childObjects;
        set => _childObjects = value ?? [];
    }

    /// <summary>Initializes empty graph ownership and full base relevance.</summary>
    protected RelevantObject()
    {
        ParentObjects = new HashSet<IRelevantObject>();
        ChildObjects = new HashSet<IRelevantObject>();
        Relevancy = 1;
    }

    /// <inheritdoc/>
    public HashSet<IRelevantObject> GetParentage(int level)
    {
        HashSet<IRelevantObject> parentageSet = new() { this };
        if (ParentObjects is null || ParentObjects.Count == 0 || level == 0 || IsLocked)
        {
            return parentageSet;
        }

        foreach (IRelevantObject relevantObject in ParentObjects)
        {
            parentageSet.UnionWith(relevantObject.GetParentage(level - 1));
        }

        return parentageSet;
    }

    /// <inheritdoc/>
    public HashSet<IRelevantObject> GetDescendants(int level)
    {
        HashSet<IRelevantObject> childrenSet = new() { this };
        if (ChildObjects is null || ChildObjects.Count == 0 || level == 0)
        {
            return childrenSet;
        }

        foreach (IRelevantObject relevantObject in ChildObjects)
        {
            childrenSet.UnionWith(relevantObject.GetDescendants(level - 1));
        }

        return childrenSet;
    }

    /// <inheritdoc/>
    public void UpdateRelevancy()
    {
        if (ParentObjects is null || ParentObjects.Count == 0)
        {
            return;
        }

        Relevancy = (Generator?.Settings?.RelevancyRatio ?? 1) * ParentObjects.Average(o => o.Relevancy);
    }

    /// <inheritdoc/>
    public void UpdateTime()
    {
        if (ParentObjects is null || ParentObjects.Count == 0)
        {
            return;
        }

        GeneratorTemporalPositioning temporalPositioning = Generator?.TemporalPositioning ?? GeneratorTemporalPositioning.Average;
        switch (temporalPositioning)
        {
            case GeneratorTemporalPositioning.Average:
                Time = ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
                break;
            case GeneratorTemporalPositioning.After:
                Time = 2 * ParentObjects.Max(o => o.Time) - ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
                break;
            case GeneratorTemporalPositioning.Before:
                Time = 2 * ParentObjects.Min(o => o.Time) - ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
                break;
            case GeneratorTemporalPositioning.Custom:
                Time = CustomTime;
                break;
            default:
                Time = ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
                break;
        }
    }

    /// <inheritdoc/>
    public virtual IRelevantObject GetLockedRelevantObject()
    {
        IRelevantObject locked = (IRelevantObject)MemberwiseClone();
        locked.Layer = null;
        locked.Generator = null;
        locked.ParentObjects = [];
        locked.ChildObjects = [];
        locked.Relevancy = 1;
        locked.Disposed = false;
        locked.IsLocked = true;
        return locked;
    }

    /// <inheritdoc/>
    public void Consume(IRelevantObject other)
    {
        if (IsLocked)
        {
            return;
        }

        if (!DoNotDispose || !ParentObjects.IsSupersetOf(other.ParentObjects))
        {
            Relevancy += other.Relevancy;
            ParentObjects.UnionWith(other.ParentObjects);
            ParentObjects.RemoveWhere(o => o.Disposed);
        }

        ChildObjects.UnionWith(other.ChildObjects);
    }

    /// <inheritdoc/>
    public abstract double DistanceTo(IRelevantObject relevantObject);
}

/// <summary>Base class for point, line, and circle objects used by the geometry engine.</summary>
public abstract class RelevantDrawable : RelevantObject, IRelevantDrawable
{
    /// <inheritdoc/>
    public abstract string PreferencesName { get; }

    /// <inheritdoc/>
    public abstract double DistanceTo(Vector2 point);

    /// <inheritdoc/>
    public abstract Vector2 NearestPoint(Vector2 point);

    /// <inheritdoc/>
    public abstract bool Intersection(IRelevantObject other, out Vector2[] intersections);
}

/// <summary>Stores the rendering preferences associated with one geometry kind.</summary>
public sealed class RelevantObjectPreferences : ICloneable
{
    /// <summary>Gets or sets the display name of this preference group.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the neutral ARGB colour consumed by a frontend renderer.</summary>
    public RgbaColour Color { get; set; }

    /// <summary>Gets or sets the base opacity multiplier.</summary>
    public double Opacity { get; set; }

    /// <summary>Gets or sets the line thickness in frontend-independent pixels.</summary>
    public double Thickness { get; set; }

    /// <summary>Gets or sets the dash pattern selected by the user.</summary>
    public DashStylesEnum Dashstyle { get; set; }

    /// <summary>Gets or sets the point radius/size when this kind supports one.</summary>
    public double Size { get; set; }

    /// <summary>Gets or sets whether the size setting applies to this geometry kind.</summary>
    public bool HasSizeOption { get; set; }

    /// <inheritdoc/>
    public object Clone() => MemberwiseClone();
}

}

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects
{

/// <summary>Represents a point generated in editor coordinates.</summary>
public sealed class RelevantPoint : RelevantDrawable
{
    /// <summary>Gets the stable preference-group name for points.</summary>
    public static string PreferencesNameStatic => "Virtual point preferences";

    /// <inheritdoc/>
    public override string PreferencesName => PreferencesNameStatic;

    /// <summary>Gets or sets the editor-space point.</summary>
    public Vector2 Child { get; set; }

    /// <summary>Creates a point for deserialization.</summary>
    public RelevantPoint() { }

    /// <summary>Creates a point at the supplied editor coordinate.</summary>
    /// <param name="vec">The editor-space coordinate.</param>
    public RelevantPoint(Vector2 vec) => Child = vec;

    /// <inheritdoc/>
    public override double DistanceTo(Vector2 point) => Vector2.Distance(Child, point);

    /// <inheritdoc/>
    public override bool Intersection(IRelevantObject other, out Vector2[] intersections)
    {
        intersections = new[] { Child };
        return other switch
        {
            RelevantPoint point => Precision.AlmostEquals(point.Child.X, Child.X) & Precision.AlmostEquals(point.Child.Y, Child.Y),
            RelevantLine line => Precision.AlmostEquals(Line2.Distance(line.Child, Child), 0),
            RelevantCircle circle => Precision.AlmostEquals(Vector2.Distance(circle.Child.Centre, Child), circle.Child.Radius),
            _ => false
        };
    }

    /// <inheritdoc/>
    public override Vector2 NearestPoint(Vector2 point) => Child;

    /// <inheritdoc/>
    public override double DistanceTo(IRelevantObject relevantObject) => relevantObject is RelevantPoint point
        ? Vector2.Distance(Child, point.Child)
        : double.PositiveInfinity;
}

/// <summary>Represents an infinite line generated in editor coordinates.</summary>
public sealed class RelevantLine : RelevantDrawable
{
    /// <summary>Gets the stable preference-group name for lines.</summary>
    public static string PreferencesNameStatic => "Virtual line preferences";

    /// <inheritdoc/>
    public override string PreferencesName => PreferencesNameStatic;

    /// <summary>Gets or sets the infinite line geometry.</summary>
    public Line2 Child { get; set; }

    /// <summary>Creates a line for deserialization.</summary>
    public RelevantLine() { }

    /// <summary>Creates a line from the supplied geometry.</summary>
    /// <param name="line">The infinite line.</param>
    public RelevantLine(Line2 line) => Child = line;

    /// <inheritdoc/>
    public override double DistanceTo(Vector2 point) => Line2.Distance(Child, point);

    /// <inheritdoc/>
    public override bool Intersection(IRelevantObject other, out Vector2[] intersections)
    {
        switch (other)
        {
            case RelevantPoint point:
                intersections = new[] { point.Child };
                return Precision.AlmostEquals(Line2.Distance(Child, point.Child), 0);
            case RelevantLine line:
                bool isIntersecting = Line2.Intersection(Child, line.Child, out Vector2 intersection);
                intersections = new[] { intersection };
                return isIntersecting;
            case RelevantCircle circle:
                return Circle.Intersection(circle.Child, Child, out intersections);
            default:
                intersections = [];
                return false;
        }
    }

    /// <inheritdoc/>
    public override Vector2 NearestPoint(Vector2 point) => Line2.NearestPoint(Child, point);

    /// <inheritdoc/>
    public override double DistanceTo(IRelevantObject relevantObject)
    {
        if (relevantObject is not RelevantLine line)
        {
            return double.PositiveInfinity;
        }

        double cosAlpha = Vector2.Dot(Child.DirectionVector, line.Child.DirectionVector) /
                          (Child.DirectionVector.Length * line.Child.DirectionVector.Length);
        double angleDiff = Math.Sqrt(10000 / (cosAlpha * cosAlpha) - 10000);
        return Line2.Distance(Child, line.Child.PositionVector) + angleDiff;
    }
}

/// <summary>Represents a circle generated in editor coordinates.</summary>
public sealed class RelevantCircle : RelevantDrawable
{
    /// <summary>Gets the stable preference-group name for circles.</summary>
    public static string PreferencesNameStatic => "Virtual circle preferences";

    /// <inheritdoc/>
    public override string PreferencesName => PreferencesNameStatic;

    /// <summary>Gets or sets the circle geometry.</summary>
    public Circle Child { get; set; }

    /// <summary>Creates a circle for deserialization.</summary>
    public RelevantCircle() { }

    /// <summary>Creates a circle from the supplied geometry.</summary>
    /// <param name="circle">The circle geometry.</param>
    public RelevantCircle(Circle circle) => Child = circle;

    /// <inheritdoc/>
    public override double DistanceTo(Vector2 point) => Math.Abs(Vector2.Distance(point, Child.Centre) - Child.Radius);

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override Vector2 NearestPoint(Vector2 point)
    {
        Vector2 diff = point - Child.Centre;
        double dist = diff.Length;
        if (Precision.AlmostEquals(dist, 0))
        {
            return Child.Centre + new Vector2(Child.Radius, 0);
        }

        return Child.Centre + diff / dist * Child.Radius;
    }

    /// <inheritdoc/>
    public override double DistanceTo(IRelevantObject relevantObject) => relevantObject is RelevantCircle circle
        ? Vector2.Distance(Child.Centre, circle.Child.Centre) + Math.Abs(Child.Radius - circle.Child.Radius)
        : double.PositiveInfinity;
}

/// <summary>Wraps a beatmap hit object as the root graph object.</summary>
public sealed class RelevantHitObject : RelevantObject
{
    /// <summary>Gets or sets the shared beatmap hit object.</summary>
    public HitObject HitObject { get; set; } = new();

    /// <inheritdoc/>
    public override double Time
    {
        get => HitObject.Time;
        set
        {
            HitObject.Time = value;
            if (ChildObjects is null)
            {
                return;
            }

            foreach (IRelevantObject relevantObject in ChildObjects)
            {
                relevantObject.UpdateTime();
            }

            Layer?.SortTimes();
        }
    }

    /// <summary>Creates a hit-object wrapper for deserialization.</summary>
    public RelevantHitObject() { }

    /// <summary>Creates a wrapper around the supplied beatmap object.</summary>
    /// <param name="hitObject">The shared hit object; it remains owned by the caller.</param>
    public RelevantHitObject(HitObject hitObject) => HitObject = hitObject;

    /// <inheritdoc/>
    public override IRelevantObject GetLockedRelevantObject()
    {
        RelevantHitObject locked = (RelevantHitObject)base.GetLockedRelevantObject();
        locked.HitObject = HitObject.DeepCopy();
        return locked;
    }

    /// <summary>Measures average squared geometry difference from another same-shaped hit object.</summary>
    /// <param name="other">The other root object.</param>
    /// <returns>The average squared coordinate difference, or positive infinity for different shapes.</returns>
    public double Difference(RelevantHitObject other)
    {
        List<Vector2> curvePoints = HitObject.CurvePoints ?? [];
        List<Vector2> otherCurvePoints = other.HitObject.CurvePoints ?? [];
        if (HitObject.ObjectType != other.HitObject.ObjectType ||
            HitObject.SliderType != other.HitObject.SliderType ||
            curvePoints.Count != otherCurvePoints.Count)
        {
            return double.PositiveInfinity;
        }

        List<double> differences = [Vector2.DistanceSquared(HitObject.Pos, other.HitObject.Pos)];
        differences.AddRange(curvePoints.Select((point, index) =>
            Vector2.DistanceSquared(point, otherCurvePoints[index])));
        return differences.Sum() / differences.Count;
    }

    /// <inheritdoc/>
    public override double DistanceTo(IRelevantObject relevantObject) => double.PositiveInfinity;
}
}

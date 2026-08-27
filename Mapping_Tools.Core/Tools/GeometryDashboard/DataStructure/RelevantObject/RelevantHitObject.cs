using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;

/// <summary>Wraps a beatmap hit object as the root graph object.</summary>
public sealed class RelevantHitObject : RelevantObject
{
    /// <summary>Creates a hit-object wrapper for deserialization.</summary>
    public RelevantHitObject() { }

    /// <summary>Creates a wrapper around the supplied beatmap object.</summary>
    /// <param name="hitObject">The shared hit object; it remains owned by the caller.</param>
    public RelevantHitObject(HitObject hitObject)
    {
        HitObject = hitObject;
    }

    /// <summary>Gets or sets the shared beatmap hit object.</summary>
    public HitObject HitObject { get; set; } = new();

    /// <inheritdoc />
    public override double Time
    {
        get => HitObject.Time;
        set
        {
            HitObject.Time = value;
            if (ChildObjects is null) return;

            foreach (var relevantObject in ChildObjects) relevantObject.UpdateTime();

            Layer?.SortTimes();
        }
    }

    /// <inheritdoc />
    public override IRelevantObject GetLockedRelevantObject()
    {
        var locked = (RelevantHitObject)base.GetLockedRelevantObject();
        locked.HitObject = HitObject.DeepCopy();
        return locked;
    }

    /// <summary>Measures average squared geometry difference from another same-shaped hit object.</summary>
    /// <param name="other">The other root object.</param>
    /// <returns>The average squared coordinate difference, or positive infinity for different shapes.</returns>
    public double Difference(RelevantHitObject other)
    {
        var curvePoints = HitObject.CurvePoints ?? [];
        var otherCurvePoints = other.HitObject.CurvePoints ?? [];
        if (HitObject.ObjectType != other.HitObject.ObjectType || HitObject.SliderType != other.HitObject.SliderType || curvePoints.Count != otherCurvePoints.Count)
            return double.PositiveInfinity;

        List<double> differences = [Vector2.DistanceSquared(HitObject.Pos, other.HitObject.Pos)];
        differences.AddRange(curvePoints.Select((point, index) =>
            Vector2.DistanceSquared(point, otherCurvePoints[index])));
        return differences.Sum() / differences.Count;
    }

    /// <inheritdoc />
    public override double DistanceTo(IRelevantObject relevantObject)
    {
        return double.PositiveInfinity;
    }
}

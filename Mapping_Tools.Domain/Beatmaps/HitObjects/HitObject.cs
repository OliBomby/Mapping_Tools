using Mapping_Tools.Domain.Beatmaps.Contexts;
using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.Beatmaps.Parsing.V14.HitObjects;
using Mapping_Tools.Domain.Beatmaps.Types;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.HitObjects;

/// <summary>
/// Base class for a hit object in osu! stable.
/// Has extra fields for easier editing and analysis.
/// </summary>
public abstract class HitObject : ContextableBase, IComparable<HitObject>, IHasPosition, IHasStartTime, IHasDuration, IHasEndPosition {
    /// <summary>
    /// Selected hit object.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Base position of hit object.
    /// </summary>
    public Vector2 Pos { get; set; }

    /// <summary>
    /// Absolute time of the hit object in milliseconds.
    /// </summary>
    public double StartTime { get; set; }

    /// <summary>
    /// Hit object forces a new combo.
    /// </summary>
    public bool NewCombo { get; set; }

    /// <summary>
    /// Extra combo skip for combo colours.
    /// By default a new combo adds one to the colour index.
    /// This value adds additional increments to the colour index each new combo.
    /// </summary>
    public virtual int ComboSkip { get; set; }

    /// <summary>
    /// How much the combo increments when a new combo is on this hit object.
    /// Ignores <see cref="ComboSkip"/>, so this is the default increment.
    /// </summary>
    public virtual int ComboIncrement => 1;

    /// <summary>
    /// The hitsounds of this hit object.
    /// </summary>
    public HitSampleInfo Hitsounds { get; set; } = new();

    /// <summary>
    /// The duration of this hit object.
    /// </summary>
    public virtual double Duration => 0;

    /// <summary>
    /// The end time of this hit object.
    /// </summary>
    public virtual double EndTime => StartTime;

    /// <summary>
    /// The end position of this hit object.
    /// </summary>
    public virtual Vector2 EndPos => Pos;

    /// <summary>
    /// Removes all hitounds and sets samplesets to auto.
    /// Also clears hitsounds from timeline objects and clears body hitsounds.
    /// </summary>
    public virtual void ResetHitsounds() {
        Hitsounds = new HitSampleInfo();
    }

    /// <summary>
    /// Gets the end time but you can truncate it to an integer.
    /// </summary>
    /// <param name="floor">Whether to truncate it to an integer.</param>
    /// <returns>The end time.</returns>
    public virtual double GetEndTime(bool floor = false) {
        return floor ? Math.Floor(EndTime + Precision.DoubleEpsilon) : EndTime;
    }

    /// <summary>
    /// </summary>
    /// <param name="deltaTime"></param>
    public virtual void MoveTime(double deltaTime) {
        StartTime += deltaTime;
    }

    /// <summary>
    /// </summary>
    /// <param name="delta"></param>
    public virtual void Move(Vector2 delta) {
        Pos += delta;
    }

    /// <summary>
    /// Apply a 2x2 transformation matrix to the hit object position.
    /// </summary>
    /// <param name="mat">The transformation matrix to apply to the position</param>
    public virtual void Transform(Matrix2 mat) {
        Pos = Matrix2.Mult(mat, Pos);
    }

    /// <summary>
    /// Determines whether this object actually has a new combo.
    /// </summary>
    /// <param name="previousHitObject">The hit object that comes directly before this hit object.</param>
    /// <param name="breaks">All the break periods in the beatmap.</param>
    /// <returns>Whether this hit object actually has a new combo.</returns>
    public bool IsActualNewCombo(HitObject? previousHitObject, IEnumerable<Break>? breaks) {
        return NewCombo ||
               this is Spinner ||
               previousHitObject == null ||
               previousHitObject is Spinner ||
               breaks is not null && breaks.Any(b =>
                   previousHitObject.StartTime <= b.EndTime && Precision.DefinitelySmaller(b.EndTime, StartTime));
    }

    /// <summary>
    /// Returns a string representation of this hit object.
    /// </summary>
    /// <returns>A string representation of this hit object</returns>
    public override string ToString() {
        return new HitObjectEncoder().Encode(this);
    }

    /// <summary>
    /// Makes a deep clone of this hit object and returns it.
    /// </summary>
    /// <returns>The deep clone of this hit object.</returns>
    public HitObject DeepClone() {
        var newHitObject = (HitObject) MemberwiseClone();

        newHitObject.Contexts = new Dictionary<Type, IContext>();
        foreach ((Type type, IContext context) in Contexts) {
            newHitObject.Contexts.Add(type, context.Copy());
        }

        // Deep clone for the types inheriting HitObject
        DeepCloneAdd(newHitObject);

        return newHitObject;
    }

    protected abstract void DeepCloneAdd(HitObject clonedHitObject);

    public int CompareTo(HitObject? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        if (Math.Abs(StartTime - other.StartTime) < Precision.DoubleEpsilon) return other.NewCombo.CompareTo(NewCombo);
        return StartTime.CompareTo(other.StartTime);
    }
}
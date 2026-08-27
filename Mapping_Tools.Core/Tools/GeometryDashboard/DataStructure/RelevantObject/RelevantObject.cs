using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.Layers;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;

/// <summary>Base implementation for generated objects and their ownership graph.</summary>
public abstract class RelevantObject : IRelevantObject
{
    private HashSet<IRelevantObject> childObjects = [];
    private double customTime;
    private bool isInheritable = true;
    private bool isLocked;
    private bool isSelected;
    private HashSet<IRelevantObject> parentObjects = [];
    private double relevancy;
    private double time;

    /// <summary>Initializes empty graph ownership and full base relevance.</summary>
    protected RelevantObject()
    {
        ParentObjects = new HashSet<IRelevantObject>();
        ChildObjects = new HashSet<IRelevantObject>();
        Relevancy = 1;
    }

    /// <summary>Gets or sets the manually supplied time used by custom positioning generators.</summary>
    public double CustomTime
    {
        get => customTime;
        set
        {
            customTime = value;
            if (Generator?.TemporalPositioning != GeneratorTemporalPositioning.Custom) return;

            UpdateTime();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Disposed) return;

        Layer?.Remove(this, false);
        Disposed = true;

        if (ParentObjects is not null)
            foreach (var relevantObject in ParentObjects)
                relevantObject.ChildObjects.Remove(this);

        if (ChildObjects is null) return;

        var objectsToDispose = ChildObjects.ToArray();
        foreach (var child in objectsToDispose) child.Dispose();
    }

    /// <inheritdoc />
    public virtual double Time
    {
        get => time;
        set
        {
            time = value;
            if (ChildObjects is null) return;

            foreach (var relevantObject in ChildObjects) relevantObject.UpdateTime();

            Layer?.SortTimes();
        }
    }

    /// <inheritdoc />
    public double Relevancy
    {
        get => isSelected ? 1 : relevancy;
        set
        {
            relevancy = value;
            if (ChildObjects is null) return;

            foreach (var relevantObject in ChildObjects) relevantObject.UpdateRelevancy();
        }
    }

    /// <inheritdoc />
    public bool Disposed { get; set; }

    /// <inheritdoc />
    public bool DoNotDispose { get; set; }

    /// <inheritdoc />
    public bool DefinitelyDispose { get; set; }

    /// <inheritdoc />
    public bool AutoPropagate { get; set; } = true;

    /// <inheritdoc />
    public virtual bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value) return;

            isSelected = value;
            if (ChildObjects is not null)
                foreach (var relevantObject in ChildObjects)
                    relevantObject.UpdateRelevancy();

            if (AutoPropagate) Layer?.NextLayer?.GenerateNewObjects(true);
        }
    }

    /// <inheritdoc />
    public bool IsLocked
    {
        get => isLocked;
        set
        {
            if (isLocked == value) return;

            isLocked = value;
            if (AutoPropagate) Layer?.NextLayer?.GenerateNewObjects(true);
        }
    }

    /// <inheritdoc />
    public bool IsInheritable
    {
        get => isInheritable;
        set
        {
            if (isInheritable == value) return;

            isInheritable = value;
            if (!AutoPropagate) return;

            if (isInheritable)
            {
                Layer?.NextLayer?.GenerateNewObjects(true);
            }
            else
            {
                var objectsToDispose = ChildObjects.ToArray();
                foreach (var child in objectsToDispose) child.Dispose();

                Layer?.NextLayer?.GenerateNewObjects(true);
            }
        }
    }

    /// <inheritdoc />
    public RelevantObjectLayer? Layer { get; set; }

    /// <inheritdoc />
    public RelevantObjectsGenerator? Generator { get; set; }

    /// <inheritdoc />
    public HashSet<IRelevantObject> ParentObjects
    {
        get => parentObjects;
        set
        {
            parentObjects = value ?? [];
            UpdateRelevancy();
            UpdateTime();
        }
    }

    /// <inheritdoc />
    public HashSet<IRelevantObject> ChildObjects
    {
        get => childObjects;
        set => childObjects = value ?? [];
    }

    /// <inheritdoc />
    public HashSet<IRelevantObject> GetParentage(int level)
    {
        HashSet<IRelevantObject> parentageSet = new() { this };
        if (ParentObjects is null || ParentObjects.Count == 0 || level == 0 || IsLocked) return parentageSet;

        foreach (var relevantObject in ParentObjects) parentageSet.UnionWith(relevantObject.GetParentage(level - 1));

        return parentageSet;
    }

    /// <inheritdoc />
    public HashSet<IRelevantObject> GetDescendants(int level)
    {
        HashSet<IRelevantObject> childrenSet = new() { this };
        if (ChildObjects is null || ChildObjects.Count == 0 || level == 0) return childrenSet;

        foreach (var relevantObject in ChildObjects) childrenSet.UnionWith(relevantObject.GetDescendants(level - 1));

        return childrenSet;
    }

    /// <inheritdoc />
    public void UpdateRelevancy()
    {
        if (ParentObjects is null || ParentObjects.Count == 0) return;

        Relevancy = (Generator?.Settings?.RelevancyRatio ?? 1) * ParentObjects.Average(o => o.Relevancy);
    }

    /// <inheritdoc />
    public void UpdateTime()
    {
        if (ParentObjects is null || ParentObjects.Count == 0) return;

        var temporalPositioning = Generator?.TemporalPositioning ?? GeneratorTemporalPositioning.Average;
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

    /// <inheritdoc />
    public virtual IRelevantObject GetLockedRelevantObject()
    {
        var locked = (IRelevantObject)MemberwiseClone();
        locked.Layer = null;
        locked.Generator = null;
        locked.ParentObjects = [];
        locked.ChildObjects = [];
        locked.Relevancy = 1;
        locked.Disposed = false;
        locked.IsLocked = true;
        return locked;
    }

    /// <inheritdoc />
    public void Consume(IRelevantObject other)
    {
        if (IsLocked) return;

        if (!DoNotDispose || !ParentObjects.IsSupersetOf(other.ParentObjects))
        {
            Relevancy += other.Relevancy;
            ParentObjects.UnionWith(other.ParentObjects);
            ParentObjects.RemoveWhere(o => o.Disposed);
        }

        ChildObjects.UnionWith(other.ChildObjects);
    }

    /// <inheritdoc />
    public abstract double DistanceTo(IRelevantObject relevantObject);
}


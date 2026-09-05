using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;

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


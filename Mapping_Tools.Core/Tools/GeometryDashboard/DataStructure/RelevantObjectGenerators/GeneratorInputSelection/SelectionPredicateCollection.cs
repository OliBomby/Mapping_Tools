using System.Text;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

/// <summary>Combines selection predicates using the legacy OR semantics.</summary>
public sealed class SelectionPredicateCollection : IEquatable<SelectionPredicateCollection>, ICloneable
{
    private List<SelectionPredicate> predicates = [];

    /// <summary>Creates an empty collection, which accepts every object.</summary>
    public SelectionPredicateCollection()
    {
    }

    /// <summary>Gets or sets the ordered predicates in this collection.</summary>
    public List<SelectionPredicate> Predicates
    {
        get => predicates;
        set => predicates = value ?? [];
    }

    /// <inheritdoc />
    public object Clone()
    {
        SelectionPredicateCollection clone = new();
        foreach (var selectionPredicate in Predicates) clone.Predicates.Add((SelectionPredicate)selectionPredicate.Clone());

        return clone;
    }

    /// <inheritdoc />
    public bool Equals(SelectionPredicateCollection? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Predicates.SequenceEqual(other.Predicates);
    }

    /// <summary>Checks whether any predicate accepts the candidate.</summary>
    /// <param name="relevantObject">The candidate object.</param>
    /// <param name="generator">The generator evaluating the candidate.</param>
    /// <returns><see langword="true" /> for an empty collection or any matching predicate.</returns>
    public bool Check(IRelevantObject relevantObject, RelevantObjectsGenerator generator)
    {
        return Predicates.Count == 0 || Predicates.Any(o => o.Check(relevantObject, generator));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder builder = new("{");
        foreach (var selectionPredicate in Predicates)
        {
            builder.Append(selectionPredicate);
            builder.Append(" OR ");
        }

        if (builder.Length >= 4) builder.Remove(builder.Length - 4, 4);

        builder.Append('}');
        return builder.ToString();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SelectionPredicateCollection collection && Equals(collection);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Predicates.GetHashCode();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection;

/// <summary>Groups relevant objects by concrete type while preserving time order per group.</summary>
public sealed class RelevantObjectCollection : Dictionary<Type, List<IRelevantObject>>
{
    /// <summary>Sorts every concrete-type list by derived object time.</summary>
    public void SortTimes()
    {
        foreach (Type key in Keys.ToArray())
        {
            this[key] = this[key].OrderBy(o => o.Time).ToList();
        }
    }

    /// <summary>Inserts an object at the first later timestamp in its type list.</summary>
    /// <param name="obj">The object to insert.</param>
    public void SortedInsert(IRelevantObject obj)
    {
        Type type = obj.GetType();
        if (TryGetValue(type, out List<IRelevantObject>? list))
        {
            // Insert the new object at the right index so time stays sorted
            int index = list.FindIndex(o => o.Time > obj.Time);
            if (index == -1)
            {
                list.Add(obj);
            }
            else
            {
                list.Insert(index, obj);
            }
        }
        else
        {
            Add(type, [obj]);
        }
    }

    /// <summary>Merges another already-time-sorted collection into this collection.</summary>
    /// <param name="other">The collection to merge.</param>
    public void MergeWith(RelevantObjectCollection other)
    {
        // Merge all types in this
        foreach (Type key in Keys.ToArray())
        {
            if (other.TryGetValue(key, out List<IRelevantObject>? otherValue))
            {
                this[key] = SortedMerge(this[key], otherValue);
            }
        }

        // Add the types that only the other has
        foreach (Type type in other.Keys.Except(Keys))
        {
            Add(type, other[type]);
        }
    }

    /// <summary>Merges two type-grouped collections without removing duplicates.</summary>
    /// <param name="collection1">The first collection.</param>
    /// <param name="collection2">The second collection.</param>
    /// <returns>A newly allocated merged collection.</returns>
    public static RelevantObjectCollection Merge(RelevantObjectCollection collection1, RelevantObjectCollection collection2)
    {
        RelevantObjectCollection result = new();
        // Merge all types in this
        foreach ((Type type, List<IRelevantObject> objects) in collection1)
        {
            result.Add(type, collection2.TryGetValue(type, out List<IRelevantObject>? other)
                ? SortedMerge(objects, other)
                : objects);
        }

        // Add the types that only the other has
        foreach (Type type in collection2.Keys.Except(collection1.Keys))
        {
            result.Add(type, collection2[type]);
        }

        return result;
    }

    /// <summary>Returns a time-sorted list containing the requested concrete types.</summary>
    /// <param name="keys">The concrete object types to include.</param>
    /// <returns>The merged list.</returns>
    public List<IRelevantObject> GetSortedSubset(IEnumerable<Type> keys)
    {
        List<IRelevantObject> result = [];
        foreach (Type key in keys)
        {
            if (TryGetValue(key, out List<IRelevantObject>? list))
            {
                result = SortedMerge(result, list);
            }
        }

        return result;
    }

    /// <summary>Filters objects using the generator's OR-combined selection predicates.</summary>
    /// <param name="predicate">The predicate collection, or null to copy all groups.</param>
    /// <param name="generator">The generator evaluating the predicates.</param>
    /// <returns>A new collection containing the matching references.</returns>
    public RelevantObjectCollection GetSubset(SelectionPredicateCollection? predicate, RelevantObjectsGenerator generator)
    {
        RelevantObjectCollection result = new();
        if (predicate is null)
        {
            foreach ((Type type, List<IRelevantObject> objects) in this)
            {
                result.Add(type, objects);
            }

            return result;
        }

        foreach ((Type type, List<IRelevantObject> objects) in this)
        {
            result.Add(type, objects.Where(o => predicate.Check(o, generator)).ToList());
        }

        return result;
    }

    /// <summary>Merges two time-sorted lists without removing duplicates.</summary>
    /// <param name="list1">The first sorted list.</param>
    /// <param name="list2">The second sorted list.</param>
    /// <returns>A newly allocated sorted list.</returns>
    public static List<IRelevantObject> SortedMerge(List<IRelevantObject> list1, List<IRelevantObject> list2)
    {
        List<IRelevantObject> newList = new(list1.Count + list2.Count);
        int index1 = 0;
        int index2 = 0;
        while (index1 < list1.Count || index2 < list2.Count)
        {
            if (index1 >= list1.Count)
            {
                newList.Add(list2[index2++]);
                continue;
            }

            if (index2 >= list2.Count)
            {
                newList.Add(list1[index1++]);
                continue;
            }

            newList.Add(list1[index1].Time < list2[index2].Time ? list1[index1++] : list2[index2++]);
        }

        return newList;
    }

    /// <summary>Finds the first same-type object closer than the supplied tolerance.</summary>
    /// <param name="obj">The object being inserted or compared.</param>
    /// <param name="acceptableDifference">The strict distance threshold.</param>
    /// <param name="similarObject">The first matching object when found.</param>
    /// <returns><see langword="true"/> when a similar object exists.</returns>
    public bool FindSimilar(IRelevantObject obj, double acceptableDifference, out IRelevantObject? similarObject)
    {
        Type type = obj.GetType();
        similarObject = TryGetValue(type, out List<IRelevantObject>? list)
            ? list.FirstOrDefault(o => obj.DistanceTo(o) < acceptableDifference)
            : null;
        return similarObject is not null;
    }

    /// <summary>Removes one object reference from its concrete-type list.</summary>
    /// <param name="relevantObject">The object to remove.</param>
    public void RemoveRelevantObject(IRelevantObject relevantObject)
    {
        if (TryGetValue(relevantObject.GetType(), out List<IRelevantObject>? list))
        {
            list.Remove(relevantObject);
        }
    }

    /// <summary>Assigns the owning layer to every contained object.</summary>
    /// <param name="layer">The owning layer.</param>
    public void SetParentLayer(RelevantObjectLayer layer)
    {
        foreach (IRelevantObject relevantObject in Values.SelectMany(list => list))
        {
            relevantObject.Layer = layer;
        }
    }

    /// <summary>Counts every object across every concrete-type list.</summary>
    /// <returns>The total count.</returns>
    public int GetCount() => this.Sum(kvp => kvp.Value.Count);

    /// <summary>Returns a new collection containing objects accepted by a predicate.</summary>
    /// <param name="predicate">The object predicate.</param>
    /// <returns>A filtered collection retaining concrete-type groups.</returns>
    public RelevantObjectCollection ObjectsWhere(Func<IRelevantObject, bool> predicate)
    {
        RelevantObjectCollection newCollection = new();
        foreach (Type key in Keys)
        {
            newCollection.Add(key, this[key].Where(predicate).ToList());
        }

        return newCollection;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools_Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools_Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools_Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools_Core.Tools.SnappingTools.DataStructure.RelevantObjectCollection {
    /// <summary>
    /// Stores sorted <see cref="IRelevantObject"/> seperated byt type.
    /// </summary>
    public class RelevantObjectCollection : Dictionary<Type, SortedSet<IRelevantObject>> {
        public IComparer<IRelevantObject> SetComparer { get; }

        public RelevantObjectCollection() : this(Comparer<IRelevantObject>.Default) { }

        public RelevantObjectCollection(IComparer<IRelevantObject> setComparer) {
            SetComparer = setComparer;
        }

        public void Add(IRelevantObject obj) {
            var type = obj.GetType();
            if (TryGetValue(type, out var set)) {
                set.Add(obj);
            } else {
                Add(type, new SortedSet<IRelevantObject>(SetComparer) {obj});
            }
        }

        /// <summary>
        /// Adds all the elements of the other collection to this collection and keeps it sorted by time if both collections were sorted by time already
        /// </summary>
        /// <param name="other">The collection to merge with</param>
        public void MergeWith(RelevantObjectCollection other) {
            // Merge all types in this
            foreach (var key in Keys) {
                if (other.TryGetValue(key, out var otherValue)) {
                    this[key].UnionWith(otherValue);
                }
            }
            // Add the types that only the other has
            foreach (var type in other.Keys.Except(Keys)) {
                Add(type, other[type]);
            }
        }

        /// <summary>
        /// Merges two collections into one new collection.
        /// The new collection gets the setComparer of the first collection.
        /// </summary>
        /// <param name="collection1"></param>
        /// <param name="collection2"></param>
        /// <returns></returns>
        public static RelevantObjectCollection Merge(RelevantObjectCollection collection1,
            RelevantObjectCollection collection2) {
            var result = new RelevantObjectCollection(collection1.SetComparer);

            // Merge all types in this
            foreach (var (type1, set1) in collection1) {
                result.Add(type1,
                    collection2.TryGetValue(type1, out var set2)
                        ? new SortedSet<IRelevantObject>(set1.Concat(set2))
                        : new SortedSet<IRelevantObject>(set1));
            }
            // Add the types that only the other has
            foreach (var type in collection2.Keys.Except(collection1.Keys)) {
                result.Add(type, new SortedSet<IRelevantObject>(collection2[type]));
            }

            return result;
        }

        public SortedSet<IRelevantObject> GetTypes(IEnumerable<Type> keys) {
            var result = new SortedSet<IRelevantObject>(SetComparer);

            foreach (var key in keys) {
                if (TryGetValue(key, out var set)) {
                    result.UnionWith(set);
                }
            }

            return result;
        }

        public RelevantObjectCollection GetSelection(SelectionPredicateCollection predicate, RelevantObjectsGenerator generator) {
            var result = new RelevantObjectCollection();

            if (predicate == null) {
                foreach (var (type, set) in this) {
                    result.Add(type, new SortedSet<IRelevantObject>(set));
                }

                return result;
            }

            foreach (var (type, set) in this) {
                result.Add(type, new SortedSet<IRelevantObject>(set.Where(o => predicate.Check(o, generator))));
            }

            return result;
        }

        /// <summary>
        /// Merges two lists of relevant objects into one. If both lists are sorted by time then the output will also be sorted by time. Duplicates will not be removed.
        /// </summary>
        /// <param name="list1">The first time-sorted list</param>
        /// <param name="list2">The second time-sorted list</param>
        /// <returns>A time-sorted list with the elements of both input lists</returns>
        [Obsolete]
        public static List<IRelevantObject> SortedMerge(List<IRelevantObject> list1, List<IRelevantObject> list2) {
            var newList = new List<IRelevantObject>(list1.Count + list2.Count);

            var index1 = 0;
            var index2 = 0;
            while (index1 < list1.Count || index2 < list2.Count) {
                if (index1 >= list1.Count) {
                    newList.Add(list2[index2++]);
                    continue;
                }

                if (index2 >= list2.Count) {
                    newList.Add(list1[index1++]);
                    continue;
                }

                newList.Add(list1[index1].Time < list2[index2].Time ? list1[index1++] : list2[index2++]);
            }

            return newList;
        }

        /// <summary>
        /// Tries to find a similar object in this collection.
        /// </summary>
        /// <param name="obj">The object to find a similar object to.</param>
        /// <param name="acceptableDifference">The maximum allowed distance between given obj and the similar obj.</param>
        /// <param name="similarObject">The similar object. This is null if nothing is found</param>
        /// <returns>Whether a similar object was found</returns>
        public bool FindSimilar(IRelevantObject obj, double acceptableDifference, out IRelevantObject similarObject) {
            var type = obj.GetType();
            similarObject = TryGetValue(type, out var list) ? list.FirstOrDefault(o => obj.DistanceTo(o) <= acceptableDifference) : null;
            return similarObject != null;
        }

        public void RemoveRelevantObject(IRelevantObject relevantObject) {
            if (TryGetValue(relevantObject.GetType(), out var list)) {
                list.Remove(relevantObject);
            }
        }

        public void SetParentLayer(RelevantObjectLayer layer) {
            foreach (var relevantObject in Values.SelectMany(list => list)) {
                relevantObject.Layer = layer;
            }
        }

        /// <summary>
        /// Returns the number of relevant objects in this collection
        /// </summary>
        /// <returns></returns>
        public int GetCount() {
            return this.Sum(kvp => kvp.Value.Count);
        }
    }
}
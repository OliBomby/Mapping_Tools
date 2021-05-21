using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection {
    public class RelevantObjectCollection : Dictionary<Type, List<IRelevantObject>> {
        public void SortTimes() {
            var keys = new List<Type>(Keys);
            foreach (var key in keys) {
                this[key] = this[key].OrderBy(o => o.Time).ToList();
            }
        }

        public void SortedInsert(IRelevantObject obj) {
            var type = obj.GetType();
            if (TryGetValue(type, out var list)) {
                // Insert the new object at the right index so time stays sorted
                var index = list.FindIndex(o => o.Time > obj.Time);
                if (index == -1) {
                    list.Add(obj);
                } else {
                    list.Insert(index, obj);
                }
            } else {
                Add(type, new List<IRelevantObject> {obj});
            }
        }

        /// <summary>
        /// Adds all the elements of the other collection to this collection and keeps it sorted by time if both collections were sorted by time already
        /// </summary>
        /// <param name="other">The collection to merge with</param>
        public void MergeWith(RelevantObjectCollection other) {
            // Merge all types in this
            var keys = new List<Type>(Keys);
            foreach (var key in keys) {
                if (other.TryGetValue(key, out var otherValue)) {
                    this[key] = SortedMerge(this[key], otherValue);
                }
            }
            // Add the types that only the other has
            foreach (var type in other.Keys.Except(Keys)) {
                Add(type, other[type]);
            }
        }

        public static RelevantObjectCollection Merge(RelevantObjectCollection collection1,
            RelevantObjectCollection collection2) {
            var result = new RelevantObjectCollection();

            // Merge all types in this
            foreach (var kvp in collection1) {
                result.Add(kvp.Key,
                    collection2.TryGetValue(kvp.Key, out var otherValue)
                        ? SortedMerge(kvp.Value, otherValue)
                        : kvp.Value);
            }
            // Add the types that only the other has
            foreach (var type in collection2.Keys.Except(collection1.Keys)) {
                result.Add(type, collection2[type]);
            }

            return result;
        }

        public List<IRelevantObject> GetSortedSubset(IEnumerable<Type> keys) {
            var result = new List<IRelevantObject>();

            foreach (var key in keys) {
                if (TryGetValue(key, out var list)) {
                    result = SortedMerge(result, list);
                }
            }

            return result;
        }

        public RelevantObjectCollection GetSubset(SelectionPredicateCollection predicate, RelevantObjectsGenerator generator) {
            var result = new RelevantObjectCollection();

            if (predicate == null) {
                foreach (var kvp in this) {
                    result.Add(kvp.Key, kvp.Value);
                }

                return result;
            }

            foreach (var kvp in this) {
                result.Add(kvp.Key, kvp.Value.Where(o => predicate.Check(o, generator)).ToList());
            }

            return result;
        }

        /// <summary>
        /// Merges two lists of relevant objects into one. If both lists are sorted by time then the output will also be sorted by time. Duplicates will not be removed.
        /// </summary>
        /// <param name="list1">The first time-sorted list</param>
        /// <param name="list2">The second time-sorted list</param>
        /// <returns>A time-sorted list with the elements of both input lists</returns>
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

        public bool FindSimilar(IRelevantObject obj, double acceptableDifference, out IRelevantObject similarObject) {
            var type = obj.GetType();
            similarObject = TryGetValue(type, out var list) ? list.FirstOrDefault(o => obj.DistanceTo(o) < acceptableDifference) : null;
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
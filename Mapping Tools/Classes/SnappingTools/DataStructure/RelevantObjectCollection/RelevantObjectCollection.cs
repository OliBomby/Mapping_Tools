using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectCollection {
    public class RelevantObjectCollection : Dictionary<Type, List<IRelevantObject>> {
        public void SortTimes() {
            foreach (var kvp in this) {
                this[kvp.Key] = kvp.Value.OrderBy(o => o.Time).ToList();
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

        public List<IRelevantObject> GetSortedSubset(IEnumerable<Type> keys) {
            var result = new List<IRelevantObject>();

            foreach (var key in keys) {
                if (TryGetValue(key, out var list)) {
                    result = SortedMerge(result, list);
                }
            }

            return result;
        }

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
    }
}
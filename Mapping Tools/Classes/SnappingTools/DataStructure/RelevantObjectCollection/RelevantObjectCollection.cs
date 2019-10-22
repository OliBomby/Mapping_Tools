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

        public virtual void SortedInsert(IRelevantObject obj) {
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
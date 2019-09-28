using System.Collections;
using System.Collections.Generic;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectCollection {
    public class RelevantObjectCollection : IEnumerable<IRelevantObject> {
        public List<IRelevantObject> Objects;

        public void SortTimes() {
            Objects.Sort((o1, o2) => o1.Time.CompareTo(o2.Time));
        }

        public void SortedInsert(IRelevantObject obj) {
            // Insert the new object at the right index so time stays sorted
            var index = Objects.FindIndex(o => o.Time > obj.Time);
            Objects.Insert(index, obj);
        }

        public IEnumerator<IRelevantObject> GetEnumerator() {
            return Objects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
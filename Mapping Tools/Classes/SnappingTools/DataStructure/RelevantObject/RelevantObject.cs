using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject {
    public abstract class RelevantObject : IRelevantObject {
        public void Dispose() {
            Layer?.Remove(this, false);
            Disposed = true;

            // Return if there are no children
            if (ChildObjects == null) {
                return;
            }

            // Kill all children
            foreach (var relevantObjectChildObject in ChildObjects) {
                relevantObjectChildObject.Dispose();
            }
        }

        private double _time;
        public double Time {
            get => _time;
            set {
                _time = value;
                ChildObjects?.ForEach(o => o.UpdateTime());
                Layer?.SortTimes();
            }
        }

        private double _relevancy;
        public double Relevancy {
            get => _relevancy;
            set {
                _relevancy = value;
                ChildObjects?.ForEach(o => o.UpdateRelevancy());
            }
        }
        public bool Disposed { get; set; }
        public RelevantObjectLayer Layer { get; set; }
        public RelevantObjectsGenerator Generator { get; set; }

        private List<IRelevantObject> _parentObjects;
        public List<IRelevantObject> ParentObjects {
            get => _parentObjects;
            set {
                _parentObjects = value;
                UpdateRelevancy();
                UpdateTime();
            }
        }

        public List<IRelevantObject> ChildObjects { get; set; }

        protected RelevantObject() {
            ParentObjects = new List<IRelevantObject>();
            ChildObjects = new List<IRelevantObject>();
        }

        public void UpdateRelevancy() {
            if (ParentObjects == null || ParentObjects.Count == 0) return;
            Relevancy = ParentObjects.Max(o => o.Relevancy);
        }

        public void UpdateTime() {
            if (ParentObjects == null || ParentObjects.Count == 0) return;
            Time = ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
        }

        public abstract double DistanceTo(IRelevantObject relevantObject);
    }
}
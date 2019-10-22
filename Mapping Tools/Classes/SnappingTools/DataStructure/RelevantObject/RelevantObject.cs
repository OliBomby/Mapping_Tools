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
                if (ChildObjects == null) return;
                foreach (var relevantObject in ChildObjects) {
                    relevantObject.UpdateTime();
                }
                Layer?.SortTimes();
            }
        }

        private double _relevancy;
        public double Relevancy {
            get => _relevancy;
            set {
                _relevancy = value;
                if (ChildObjects == null) return;
                foreach (var relevantObject in ChildObjects) {
                    relevantObject.UpdateRelevancy();
                }
            }
        }
        public bool Disposed { get; set; }

        private bool _isSelected { get; set; }
        public virtual bool IsSelected {
            get => _isSelected; 
            set {
                _isSelected = value;
                if (ChildObjects == null) return;
                foreach (var relevantObject in ChildObjects) {
                    relevantObject.UpdateSelected();
                }
            }
        }

        public RelevantObjectLayer Layer { get; set; }
        public RelevantObjectsGenerator Generator { get; set; }

        private HashSet<IRelevantObject> _parentObjects;
        public HashSet<IRelevantObject> ParentObjects {
            get => _parentObjects;
            set {
                _parentObjects = value;
                UpdateRelevancy();
                UpdateTime();
            }
        }

        public HashSet<IRelevantObject> ChildObjects { get; set; }

        protected RelevantObject() {
            ParentObjects = new HashSet<IRelevantObject>();
            ChildObjects = new HashSet<IRelevantObject>();
        }

        public void UpdateRelevancy() {
            if (ParentObjects == null || ParentObjects.Count == 0) return;
            Relevancy = ParentObjects.Max(o => o.Relevancy);
        }

        public void UpdateTime() {
            if (ParentObjects == null || ParentObjects.Count == 0) return;
            Time = ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
        }

        public void UpdateSelected() {
            if (ParentObjects == null || ParentObjects.Count == 0) return;
            IsSelected = ParentObjects.Any(o => o.IsSelected);
        }
        
        public void Consume(IRelevantObject other) {
            ParentObjects.UnionWith(other.ParentObjects);
        }

        public abstract double DistanceTo(IRelevantObject relevantObject);
    }
}
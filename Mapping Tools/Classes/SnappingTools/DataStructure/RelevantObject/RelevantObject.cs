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

            // Remove this from parents
            if (ParentObjects != null) {
                foreach (var relevantObject in ParentObjects) {
                    relevantObject.ChildObjects.Remove(this);
                }
            }

            // Return if there are no children
            if (ChildObjects == null) {
                return;
            }

            // Kill all children
            var objectsToDispose = ChildObjects.ToArray();
            foreach (var t in objectsToDispose) {
                t.Dispose();
            }
        }

        private double _time;
        public virtual double Time {
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

        /// <summary>
        /// Returns a set with all the parents of this object and all the parents' parents and this object itself
        /// </summary>
        public HashSet<IRelevantObject> GetParentage() {
            var parentageSet = new HashSet<IRelevantObject> {this};

            if (ParentObjects == null || ParentObjects.Count == 0) {
                return parentageSet;
            }

            foreach (var relevantObject in ParentObjects) {
                parentageSet.UnionWith(relevantObject.GetParentage());
            }

            return parentageSet;
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
            ChildObjects.UnionWith(other.ChildObjects);
        }

        public abstract double DistanceTo(IRelevantObject relevantObject);
    }
}
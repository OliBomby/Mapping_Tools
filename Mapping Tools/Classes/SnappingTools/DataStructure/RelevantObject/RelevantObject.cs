using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject {
    public abstract class RelevantObject : IRelevantObject {
        public void Dispose() {
            if (Disposed) {
                return;
            }

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

        private bool _isSelected;
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

        public bool IsLocked { get; set; }
        public bool IsInheritable { get; set; } = true;

        public RelevantObjectLayer Layer { get; set; }
        public RelevantObjectsGenerator Generator { get; set; }

        private HashSet<IRelevantObject> _parentObjects;
        public HashSet<IRelevantObject> ParentObjects {
            get => _parentObjects;
            set {
                _parentObjects = value;
                UpdateRelevancy();
                UpdateTime();
                UpdateSelected();
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

        /// <summary>
        /// Returns a set with all the children of this object and all the children' children and this object itself
        /// </summary>
        public HashSet<IRelevantObject> GetDescendants() {
            var childrenSet = new HashSet<IRelevantObject> {this};

            if (ChildObjects == null || ChildObjects.Count == 0) {
                return childrenSet;
            }

            foreach (var relevantObject in ChildObjects) {
                childrenSet.UnionWith(relevantObject.GetDescendants());
            }

            return childrenSet;
        }

        public void UpdateRelevancy() {
            if (ParentObjects == null || ParentObjects.Count == 0) return;
            Relevancy = ParentObjects.Max(o => o.Relevancy);
        }

        public void UpdateTime() {
            if (ParentObjects == null || ParentObjects.Count == 0) return;

            var temporalPositioning = Generator?.TemporalPositioning ?? GeneratorTemporalPositioning.Average;

            switch (temporalPositioning) {
                case GeneratorTemporalPositioning.Average:
                    Time = ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
                    break;
                case GeneratorTemporalPositioning.After:
                    Time = 2 * ParentObjects.Max(o => o.Time) - ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
                    break;
                case GeneratorTemporalPositioning.Before:
                    Time = 2 * ParentObjects.Min(o => o.Time) - ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
                    break;
                default:
                    Time = ParentObjects.Sum(o => o.Time) / ParentObjects.Count;
                    break;
            }
        }

        public void UpdateSelected() {
            if (ParentObjects == null || ParentObjects.Count == 0) return;
            IsSelected = ParentObjects.Any(o => o.IsSelected);
        }

        /// <summary>
        /// Makes a copy of this relevant object which is locked and is disconnected from the object structure.
        /// </summary>
        /// <returns></returns>
        public IRelevantObject GetLockedRelevantObject() {
            var locked = (IRelevantObject)MemberwiseClone();

            locked.Layer = null;
            locked.Generator = null;
            locked.ParentObjects.Clear();
            locked.ChildObjects.Clear();

            locked.Relevancy = 1;

            locked.Disposed = false;
            locked.IsSelected = false;
            locked.IsLocked = true;

            return locked;
        }
        
        public void Consume(IRelevantObject other) {
            ParentObjects.UnionWith(other.ParentObjects);
            ChildObjects.UnionWith(other.ChildObjects);
        }

        public abstract double DistanceTo(IRelevantObject relevantObject);
    }
}
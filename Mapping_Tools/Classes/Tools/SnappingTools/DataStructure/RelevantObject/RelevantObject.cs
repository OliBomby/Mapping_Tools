using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject {
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

        public bool DoNotDispose { get; set; }

        public bool DefinitelyDispose { get; set; }

        public bool AutoPropagate { get; set; } = true;

        private bool _isSelected;
        public virtual bool IsSelected {
            get => _isSelected;
            set {
                if (_isSelected == value) return;
                _isSelected = value;
                if (!AutoPropagate) return;
                Layer?.NextLayer?.GenerateNewObjects(true);
            }
        }

        private bool _isLocked;
        public bool IsLocked {
            get => _isLocked;
            set {
                if (_isLocked == value) return;
                _isLocked = value;
                if (!AutoPropagate) return;
                Layer?.NextLayer?.GenerateNewObjects(true);
            }
        }

        private bool _isInheritable = true;
        public bool IsInheritable {
            get => _isInheritable;
            set {
                if (_isInheritable == value) return;
                _isInheritable = value;
                if (!AutoPropagate) return;
                if (_isInheritable) {
                    Layer?.NextLayer?.GenerateNewObjects(true);
                } else {
                    var objectsToDispose = ChildObjects.ToArray();
                    foreach (var t in objectsToDispose) {
                        t.Dispose();
                    }
                    Layer?.NextLayer?.GenerateNewObjects(true);
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

            Relevancy = 1;
        }

        /// <summary>
        /// Returns a set with all the parents of this object and all the parents' parents and this object itself
        /// </summary>
        public HashSet<IRelevantObject> GetParentage(int level) {
            var parentageSet = new HashSet<IRelevantObject> {this};

            if (ParentObjects == null || ParentObjects.Count == 0 || level == 0 || IsLocked) {
                return parentageSet;
            }
            
            foreach (var relevantObject in ParentObjects) {
                parentageSet.UnionWith(relevantObject.GetParentage(level - 1));
            }

            return parentageSet;
        }

        /// <summary>
        /// Returns a set with all the children of this object and all the children' children and this object itself
        /// </summary>
        public HashSet<IRelevantObject> GetDescendants(int level) {
            var childrenSet = new HashSet<IRelevantObject> {this};

            if (ChildObjects == null || ChildObjects.Count == 0 || level == 0) {
                return childrenSet;
            }

            foreach (var relevantObject in ChildObjects) {
                childrenSet.UnionWith(relevantObject.GetDescendants(level - 1));
            }

            return childrenSet;
        }

        public void UpdateRelevancy() {
            if (ParentObjects == null || ParentObjects.Count == 0) return;
            Relevancy = (Generator?.Settings?.RelevancyRatio ?? 1) * ParentObjects.Average(o => o.Relevancy);
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
            locked.IsLocked = true;

            return locked;
        }
        
        public void Consume(IRelevantObject other) {
            if (IsLocked) return;
            ParentObjects.UnionWith(other.ParentObjects);
            ChildObjects.UnionWith(other.ChildObjects);
        }

        public abstract double DistanceTo(IRelevantObject relevantObject);
    }
}
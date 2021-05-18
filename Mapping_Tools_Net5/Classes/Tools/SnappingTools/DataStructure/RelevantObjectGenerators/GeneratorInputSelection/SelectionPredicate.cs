using System;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection {
    public class SelectionPredicate : BindableBase, IEquatable<SelectionPredicate>, ICloneable {
        private bool _needSelected;
        private bool _needLocked;
        private bool _needGeneratedByThis;
        private bool _needGeneratedNotByThis;
        private double _minRelevancy;
        public bool NeedSelected { get => _needSelected; set => Set(ref _needSelected, value); }
        public bool NeedLocked { get => _needLocked; set => Set(ref _needLocked, value); }
        public bool NeedGeneratedByThis { get => _needGeneratedByThis; set => Set(ref _needGeneratedByThis, value); }
        public bool NeedGeneratedNotByThis { get => _needGeneratedNotByThis; set => Set(ref _needGeneratedNotByThis, value); }
        public double MinRelevancy { get => _minRelevancy; set => Set(ref _minRelevancy, value); }

        public bool Check(IRelevantObject relevantObject, RelevantObjectsGenerator generator) {
            if (NeedSelected && !relevantObject.IsSelected) return false;
            if (NeedLocked && !relevantObject.IsLocked) return false;
            if (NeedGeneratedByThis && (relevantObject.Generator == null || relevantObject.Generator != generator)) return false;
            if (NeedGeneratedNotByThis && relevantObject.Generator != null && relevantObject.Generator == generator) return false;
            return !(relevantObject.Relevancy < MinRelevancy);
        }

        public override string ToString() {
            return $@"NeedSelected: {NeedSelected}, NeedLocked: {NeedLocked}, NeedGeneratedByThis: {NeedGeneratedByThis}, NeedGeneratedNotByThis: {NeedGeneratedNotByThis}, MinRelevancy: {MinRelevancy}";
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public bool Equals(SelectionPredicate other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _needSelected == other._needSelected && _needLocked == other._needLocked && _needGeneratedByThis == other._needGeneratedByThis && _needGeneratedNotByThis == other._needGeneratedNotByThis && _minRelevancy.Equals(other._minRelevancy);
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SelectionPredicate) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = _needSelected.GetHashCode();
                hashCode = (hashCode * 397) ^ _needLocked.GetHashCode();
                hashCode = (hashCode * 397) ^ _needGeneratedByThis.GetHashCode();
                hashCode = (hashCode * 397) ^ _needGeneratedNotByThis.GetHashCode();
                hashCode = (hashCode * 397) ^ _minRelevancy.GetHashCode();
                return hashCode;
            }
        }
    }
}
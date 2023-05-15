using System;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection {
    public class SelectionPredicate : BindableBase, IEquatable<SelectionPredicate>, ICloneable {
        private bool needSelected;
        private bool needLocked;
        private bool needGeneratedByThis;
        private bool needGeneratedNotByThis;
        private double minRelevancy;
        public bool NeedSelected { get => needSelected; set => Set(ref needSelected, value); }
        public bool NeedLocked { get => needLocked; set => Set(ref needLocked, value); }
        public bool NeedGeneratedByThis { get => needGeneratedByThis; set => Set(ref needGeneratedByThis, value); }
        public bool NeedGeneratedNotByThis { get => needGeneratedNotByThis; set => Set(ref needGeneratedNotByThis, value); }
        public double MinRelevancy { get => minRelevancy; set => Set(ref minRelevancy, value); }

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
            return needSelected == other.needSelected && needLocked == other.needLocked && needGeneratedByThis == other.needGeneratedByThis && needGeneratedNotByThis == other.needGeneratedNotByThis && minRelevancy.Equals(other.minRelevancy);
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SelectionPredicate) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = needSelected.GetHashCode();
                hashCode = (hashCode * 397) ^ needLocked.GetHashCode();
                hashCode = (hashCode * 397) ^ needGeneratedByThis.GetHashCode();
                hashCode = (hashCode * 397) ^ needGeneratedNotByThis.GetHashCode();
                hashCode = (hashCode * 397) ^ minRelevancy.GetHashCode();
                return hashCode;
            }
        }
    }
}
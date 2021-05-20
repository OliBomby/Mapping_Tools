using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection {
    public class SelectionPredicateCollection : IEquatable<SelectionPredicateCollection>, ICloneable {
        public ObservableCollection<SelectionPredicate> Predicates { get; set; }

        public SelectionPredicateCollection() {
            Predicates = new ObservableCollection<SelectionPredicate>();
        }

        public bool Check(IRelevantObject relevantObject, RelevantObjectsGenerator generator) {
            return Predicates.Count == 0 || Predicates.Any(o => o.Check(relevantObject, generator));
        }

        public override string ToString() {
            StringBuilder builder = new StringBuilder();
            builder.Append('{');
            foreach (var selectionPredicate in Predicates) {
                builder.Append(selectionPredicate);
                builder.Append(" OR ");
            }

            if (builder.Length >= 4) {
                builder.Remove(builder.Length - 4, 4);
            }
            builder.Append('}');

            return builder.ToString();
        }

        public bool Equals(SelectionPredicateCollection other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (Predicates.Count != other.Predicates.Count) return false;
            return !Predicates.Where((t, i) => !t.Equals(other.Predicates[i])).Any();
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((SelectionPredicateCollection) obj);
        }

        public override int GetHashCode() {
            return Predicates.GetHashCode();
        }

        public object Clone() {
            var clone = new SelectionPredicateCollection();
            foreach (var selectionPredicate in Predicates) {
                clone.Predicates.Add((SelectionPredicate)selectionPredicate.Clone());
            }

            return clone;
        }
    }
}
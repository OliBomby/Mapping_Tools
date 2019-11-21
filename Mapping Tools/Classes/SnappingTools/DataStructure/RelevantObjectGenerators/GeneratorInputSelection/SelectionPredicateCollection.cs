using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection {
    public class SelectionPredicateCollection {
        public ObservableCollection<SelectionPredicate> Predicates { get; set; }

        public SelectionPredicateCollection() {
            Predicates = new ObservableCollection<SelectionPredicate>();
        }

        public bool Check(IRelevantObject relevantObject, RelevantObjectsGenerator generator) {
            return Predicates.Count == 0 || Predicates.Any(o => o.Check(relevantObject, generator));
        }
    }
}
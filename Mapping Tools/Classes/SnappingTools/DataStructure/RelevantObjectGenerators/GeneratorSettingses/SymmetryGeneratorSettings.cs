using System.ComponentModel;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses {
    public class SymmetryGeneratorSettings : GeneratorSettings {
        private SelectionPredicateCollection _axisInputPredicate;
        [DisplayName("Axis Input Selection")]
        [Description("Specifies extra rules that virtual lines need to obey to be used as the axis by this generator.")]
        public SelectionPredicateCollection AxisInputPredicate {
            get => _axisInputPredicate;
            set => Set(ref _axisInputPredicate, value);
        }
        
        private SelectionPredicateCollection _otherInputPredicate;
        [DisplayName("Other Input Selection")]
        [Description("Specifies extra rules that virtual objects need to obey to get mirrored by this generator.")]
        public SelectionPredicateCollection OtherInputPredicate {
            get => _otherInputPredicate;
            set => Set(ref _otherInputPredicate, value);
        }
    }
}
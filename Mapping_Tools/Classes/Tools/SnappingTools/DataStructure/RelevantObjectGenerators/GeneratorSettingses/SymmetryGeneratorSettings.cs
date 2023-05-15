using System.ComponentModel;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses {
    public class SymmetryGeneratorSettings : GeneratorSettings {
        private SelectionPredicateCollection axisInputPredicate;
        [DisplayName("Axis Input Selection")]
        [Description("Specifies extra rules that virtual lines need to obey to be used as the axis by this generator.")]
        public SelectionPredicateCollection AxisInputPredicate {
            get => axisInputPredicate;
            set => Set(ref axisInputPredicate, value);
        }
        
        private SelectionPredicateCollection otherInputPredicate;
        [DisplayName("Other Input Selection")]
        [Description("Specifies extra rules that virtual objects need to obey to get mirrored by this generator.")]
        public SelectionPredicateCollection OtherInputPredicate {
            get => otherInputPredicate;
            set => Set(ref otherInputPredicate, value);
        }

        public SymmetryGeneratorSettings() {
            AxisInputPredicate = new SelectionPredicateCollection();
            OtherInputPredicate = new SelectionPredicateCollection();
        }

        public override object Clone() {
            return new SymmetryGeneratorSettings {Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep, 
                RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
                InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(),
                AxisInputPredicate = (SelectionPredicateCollection)AxisInputPredicate.Clone(),
                OtherInputPredicate = (SelectionPredicateCollection)OtherInputPredicate.Clone()
            };
        }
    }
}
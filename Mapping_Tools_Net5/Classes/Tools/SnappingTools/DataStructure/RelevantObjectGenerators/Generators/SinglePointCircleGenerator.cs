using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SinglePointCircleGenerator : RelevantObjectsGenerator {
        public override string Name => "Circle from Single Point";
        public override string Tooltip => "Generates circles with a specified radius on every virtual point.";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        private SinglePointCircleGeneratorSettings MySettings => (SinglePointCircleGeneratorSettings) Settings;

        /// <summary>
        /// Initializes SinglePointCircleGenerator with a custom settings object
        /// </summary>
        public SinglePointCircleGenerator() : base(new SinglePointCircleGeneratorSettings()) {
            Settings.Generator = this;

            Settings.IsActive = false;
            Settings.IsDeep = false;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true });
            MySettings.Radius = 100;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantCircle GetRelevantObjects(RelevantPoint point) {
            return new RelevantCircle(new Circle(point.Child, MySettings.Radius));
        }
    }
}

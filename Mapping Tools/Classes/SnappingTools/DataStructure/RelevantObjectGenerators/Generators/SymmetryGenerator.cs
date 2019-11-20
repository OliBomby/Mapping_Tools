using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SymmetryGenerator : RelevantObjectsGenerator {
        public override string Name => "Symmetries from Lines";
        public override string Tooltip => "Mirrors any virtual objects over a virtual line where the virtual line is the symmetry axis. In the settings you can set extra rules for selecting symmetry axis'.";
        public override GeneratorType GeneratorType => GeneratorType.Advanced;

        private SymmetryGeneratorSettings MySettings => (SymmetryGeneratorSettings) Settings;

        /// <summary>
        /// Initializes SymmetryGenerator with a custom settings object
        /// </summary>
        public SymmetryGenerator() : base(new SymmetryGeneratorSettings()) {
            Settings.Generator = this;

            MySettings.AxisInputPredicate = new SelectionPredicateCollection();
            MySettings.AxisInputPredicate.Predicates.Add(new SelectionPredicate {NeedLocked = true});
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantLine axis, RelevantPoint point) {
            return !MySettings.AxisInputPredicate.Check(axis, this) ? null : 
                new RelevantPoint(Vector2.Mirror(point.Child, axis.Child));
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantLine axis, RelevantLine line) {
            return !MySettings.AxisInputPredicate.Check(axis, this) ? null : 
                new RelevantLine(Line2.FromPoints(Vector2.Mirror(line.Child.PositionVector, axis.Child), 
                    Vector2.Mirror(line.Child.PositionVector + line.Child.DirectionVector, axis.Child)));
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantCircle GetRelevantObjects(RelevantLine axis, RelevantCircle circle) {
            return !MySettings.AxisInputPredicate.Check(axis, this) ? null : 
                new RelevantCircle(new Circle(Vector2.Mirror(circle.Child.Centre, axis.Child), circle.Child.Radius));
        }
    }
}

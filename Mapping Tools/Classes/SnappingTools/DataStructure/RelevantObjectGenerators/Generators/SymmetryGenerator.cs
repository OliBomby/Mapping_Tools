using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SymmetryGenerator : RelevantObjectsGenerator {
        public override string Name => "Symmetries from Lines";
        public override string Tooltip => "Mirrors any virtual objects over a virtual line where the virtual line is the symmetry axis. In the settings you can set extra rules for selecting symmetry axis'.";
        public override GeneratorType GeneratorType => GeneratorType.Advanced;

        /// <summary>
        /// Initializes SymmetryGenerator with a custom settings object
        /// </summary>
        public SymmetryGenerator() : base(new SymmetryGeneratorSettings()) {
            Settings.Generator = this;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantLine axis, RelevantPoint point) {
            return new RelevantPoint(Vector2.Mirror(point.Child, axis.Child));
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantLine axis, RelevantLine line) {
            return new RelevantLine(Line2.FromPoints(Vector2.Mirror(line.Child.PositionVector, axis.Child), Vector2.Mirror(line.Child.PositionVector + line.Child.DirectionVector, axis.Child)));
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantCircle GetRelevantObjects(RelevantLine axis, RelevantCircle circle) {
            return new RelevantCircle(new Circle(Vector2.Mirror(circle.Child.Centre, axis.Child), circle.Child.Radius));
        }
    }
}

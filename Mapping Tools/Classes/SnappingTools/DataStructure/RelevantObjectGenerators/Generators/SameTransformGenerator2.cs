using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using System;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SameTransformGenerator2 : RelevantObjectsGenerator {
        public override string Name => "Successor of 2 Points";
        public override string Tooltip => "Takes 2 virtual points and calculates the next virtual point using the same velocity.";
        public override GeneratorType GeneratorType => GeneratorType.Advanced;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;

        public SameTransformGenerator2() {
            Settings.IsSequential = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            // Get the vectors between the points
            var a = point2.Child - point1.Child;

            // Return null if length of a is zero
            if (Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon) {
                return null;
            }

            return new RelevantPoint(point2.Child + a);
        }
    }
}

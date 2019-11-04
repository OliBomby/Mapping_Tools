using System;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SameTransformGenerator : RelevantObjectsGenerator {
        public override string Name => "Same Transformation Generator";
        public override string Tooltip => "Takes 3 virtual points and predicts the next virtual point using the transformation matrix of the previous 3 virtual points.";
        public override GeneratorType GeneratorType => GeneratorType.Assistants;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;

        public SameTransformGenerator() {
            Settings.IsSequential = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetRelevantObjects(RelevantPoint point1, RelevantPoint point2, RelevantPoint point3) {
            // Get the vectors between the points
            var a = point2.Child - point1.Child;
            var b = point3.Child - point2.Child;

            // Return null if length of a is zero
            if (Math.Abs(a.X) < double.Epsilon && Math.Abs(a.Y) < double.Epsilon) {
                return null;
            }

            // Calculate the next point
            Vector2 newPoint = Vector2.ComplexProduct(b, Vector2.ComplexQuotient(b, a)) + point3.Child;

            return new RelevantPoint(newPoint);
        }
    }
}

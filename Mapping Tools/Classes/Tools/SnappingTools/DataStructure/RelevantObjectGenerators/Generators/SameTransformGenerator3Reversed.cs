using System;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class SameTransformGenerator3Reversed : RelevantObjectsGenerator {
        public override string Name => "Successor of 3 Points Reversed";
        public override string Tooltip => "Takes 3 virtual points and calculates the next virtual point using the same velocity change and opposite angle.";
        public override GeneratorType GeneratorType => GeneratorType.Advanced;
        public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.After;

        public SameTransformGenerator3Reversed() {
            Settings.IsSequential = true;
            Settings.IsDeep = true;
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedSelected = true});
            Settings.InputPredicate.Predicates.Add(new SelectionPredicate {NeedGeneratedByThis = true});
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
            var diff = Vector2.ComplexQuotient(b, a);
            diff.Y = -diff.Y;
            Vector2 newPoint = Vector2.ComplexProduct(b, diff) + point3.Child;

            return new RelevantPoint(newPoint);
        }
    }
}

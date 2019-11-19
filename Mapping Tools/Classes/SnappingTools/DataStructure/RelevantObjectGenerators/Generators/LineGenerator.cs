﻿using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class LineGenerator : RelevantObjectsGenerator {
        public override string Name => "Lines by Two Points";
        public override string Tooltip => "Takes a pair of virtual points and generates a virtual line that connects the two.";
        public override GeneratorType GeneratorType => GeneratorType.Intermediate;

        public LineGenerator() {
            Settings.IsSequential = true;
        }

        [RelevantObjectsGeneratorMethod]
        public RelevantLine GetRelevantObjects(RelevantPoint point1, RelevantPoint point2) {
            return new RelevantLine(Line2.FromPoints(point1.Child, point2.Child));
        }
    }
}

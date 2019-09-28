using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class ParallelismGenerator : RelevantObjectsGenerator, IGenerateLinesFromRelevantObjects {
        public override string Name => "Parallel Line Calculator";
        public override string Tooltip => "Takes a pair of line and point and generates a virtual line across the point that is parallel to the line.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        private static RelevantLine MakeParallelLine(Line2 line, Vector2 point) {
            return new RelevantLine(new Line2(point, line.DirectionVector));
        }

        public List<RelevantLine> GetRelevantObjects(List<RelevantPoint> points, List<RelevantLine> lines, List<RelevantCircle> circles) {
            return (from line in lines from point in points select MakeParallelLine(line.Child, point.Child)).ToList();
        }
    }
}

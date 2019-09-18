using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class ParallelismGenerator : RelevantObjectsGenerator, IGenerateLinesFromRelevantObjects {
        public override string Name => "Parallel Line Calculator";
        public override string Tooltip => "Takes a pair of line and point and generates a virtual line parallel to the line on top of the point.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        private static RelevantLine MakeParallelLine(Line line, Vector2 point) {
            return new RelevantLine(new Line(line.A, line.B, line.A * point.X + line.B * point.Y));
        }

        public List<RelevantLine> GetRelevantObjects(List<RelevantPoint> points, List<RelevantLine> lines, List<RelevantCircle> circles) {
            return (from line in lines from point in points select MakeParallelLine(line.child, point.child)).ToList();
        }
    }
}

using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class LineGenerator : RelevantObjectsGenerator, IGenerateLinesFromRelevantObjects {
        public override string Name => "Virtual Lines from Virtual Points Calculator";
        public override string Tooltip => "Takes a pair of virtual points and generates a virtual line that connects the two.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        public List<RelevantLine> GetRelevantObjects(List<RelevantPoint> points, List<RelevantLine> lines, List<RelevantCircle> circles) {
            var newObjects = new List<RelevantLine>();

            for (var i = 0; i < points.Count; i++) {
                for (var k = i + 1; k < points.Count; k++) {
                    var obj1 = points[i];
                    var obj2 = points[k];

                    Line2 line = Line2.FromPoints(obj1.Child, obj2.Child);
                    newObjects.Add(new RelevantLine(line));
                }
            }

            return newObjects;
        }
    }
}

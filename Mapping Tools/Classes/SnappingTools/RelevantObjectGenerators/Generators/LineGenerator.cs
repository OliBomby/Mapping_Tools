using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class LineGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromRelevantPoints {
        public override string Name => "Lines between Two Points Generator";
        public override string Tooltip => "Takes a pair of virtual points and generates a virtual line that connects the two.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        public List<IRelevantObject> GetRelevantObjects(List<RelevantPoint> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            for (int i = 0; i < objects.Count; i++) {
                for (int k = i + 1; k < objects.Count; k++) {
                    var obj1 = objects[i];
                    var obj2 = objects[k];

                    Line2 line = new Line2(obj1.child, obj2.child);
                    newObjects.Add(new RelevantLine(line));
                }
            }

            return newObjects;
        }
    }
}

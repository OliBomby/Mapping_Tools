using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class ParallelismGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromRelevantObjects {
        public override string Name => "Parallel Line Calculator";
        public override string Tooltip => "Takes a pair of line and point and generates a virtual line parallel to the line on top of the point.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        public List<IRelevantObject> GetRelevantObjects(List<IRelevantObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            for (int i = 0; i < objects.Count; i++) {
                for (int k = i + 1; k < objects.Count; k++) {
                    var obj1 = objects[i];
                    var obj2 = objects[k];

                    if (obj1 is RelevantLine line && obj2 is RelevantPoint point)
                    {
                        newObjects.Add(MakeParallelLine(line.child, point.child));
                    } else
                    if (obj2 is RelevantLine line2 && obj1 is RelevantPoint point2) {
                        newObjects.Add(MakeParallelLine(line2.child, point2.child));
                    }
                }
            }

            return newObjects;
        }

        private static RelevantLine MakeParallelLine(Line2 line, Vector2 point) {
            return new RelevantLine(new Line2(line.A, line.B, line.A * point.X + line.B * point.Y));
        }
    }
}

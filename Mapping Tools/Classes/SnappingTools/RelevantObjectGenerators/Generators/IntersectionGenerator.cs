using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class IntersectionGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromRelevantObjects {
        public override string Name => "Intersection Point Calculator";
        public override string Tooltip => "Takes a pair of virtual lines or circles and generates a virtual point on each of their intersections.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        public List<IRelevantObject> GetRelevantObjects(List<IRelevantObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            for (int i = 0; i < objects.Count; i++) {
                for (int k = i + 1; k < objects.Count; k++) {
                    var obj1 = objects[i];
                    var obj2 = objects[k];

                    // I don't want to intersect points
                    if (obj1 is RelevantPoint || obj2 is RelevantPoint) continue;

                    if (obj1.Intersection(obj2, out Vector2[] intersections)) {
                        foreach (Vector2 v in intersections) {
                            newObjects.Add(new RelevantPoint(v));
                        }
                    }
                }
            }

            return newObjects;
        }
    }
}

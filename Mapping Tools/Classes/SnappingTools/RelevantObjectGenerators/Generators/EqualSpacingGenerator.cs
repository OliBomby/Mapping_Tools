using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class EqualSpacingGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromRelevantPoints {
        public new string Name => "Equal Spacing Generator";
        public new GeneratorType GeneratorType => GeneratorType.Polygons;

        public List<IRelevantObject> GetRelevantObjects(List<RelevantPoint> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            for (int i = 0; i < objects.Count; i++) {
                for (int k = i + 1; k < objects.Count; k++) {
                    var obj1 = objects[i];
                    var obj2 = objects[k];

                    var radius = (obj2.child - obj1.child).Length;
                    newObjects.Add(new RelevantCircle(new Circle(obj1.child, radius)));
                    newObjects.Add(new RelevantCircle(new Circle(obj2.child, radius)));
                }
            }

            return newObjects;
        }
    }
}

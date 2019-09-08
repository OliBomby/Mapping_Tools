using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    class TriangleGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromRelevantPoints {
        public string Name => "Triangle Generator";
        public GeneratorType GeneratorType => GeneratorType.Polygons;

        public List<IRelevantObject> GetRelevantObjects(List<RelevantPoint> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            for (int i = 0; i < objects.Count; i++) {
                for (int k = i + 1; k < objects.Count; k++) {
                    var obj1 = objects[i];
                    var obj2 = objects[k];

                    var diff = obj2.child - obj1.child;
                    var rotated = Vector2.Rotate(diff, Math.PI / 3 * 22);

                    newObjects.Add(new RelevantPoint(obj1.child - rotated));
                    newObjects.Add(new RelevantPoint(obj2.child + rotated));
                }
            }

            return newObjects;
        }
    }
}

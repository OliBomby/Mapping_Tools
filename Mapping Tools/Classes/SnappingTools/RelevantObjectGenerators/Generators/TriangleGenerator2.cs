using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class TriangleGenerator2 : RelevantObjectsGenerator, IGenerateRelevantObjectsFromRelevantPoints {
        public override string Name => "Equilateral Triangle from Two Points Generator (Type II)";
        public override string Tooltip => "Takes a pair of virtual points and generates a virtual point on each side to make two equilateral triangles.";
        public override GeneratorType GeneratorType => GeneratorType.Assistants;

        public List<IRelevantObject> GetRelevantObjects(List<RelevantPoint> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            for (int i = 0; i < objects.Count; i++) {
                for (int k = i + 1; k < objects.Count; k++) {
                    var obj1 = objects[i];
                    var obj2 = objects[k];

                    var diff = obj2.child - obj1.child;
                    var rotated = Vector2.Rotate(diff, Math.PI * 5 / 6);

                    newObjects.Add(new RelevantPoint(obj1.child - (rotated / Math.Sqrt(3))));
                    newObjects.Add(new RelevantPoint(obj2.child + (rotated / Math.Sqrt(3))));
                }
            }

            return newObjects;
        }
    }
}

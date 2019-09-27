using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantDrawable;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class TriangleGenerator2 : RelevantObjectsGenerator, IGeneratePointsFromRelevantObjects {
        public override string Name => "Equilateral Triangle from Two Points (Type II)";
        public override string Tooltip => "Takes a pair of virtual points and generates a virtual point on each side to make two equilateral triangles.";
        public override GeneratorType GeneratorType => GeneratorType.Assistants;

        public List<RelevantPoint> GetRelevantObjects(List<RelevantPoint> points, List<RelevantLine> lines, List<RelevantCircle> circles) {
            var newObjects = new List<RelevantPoint>();

            for (var i = 0; i < points.Count; i++) {
                for (var k = i + 1; k < points.Count; k++) {
                    var obj1 = points[i];
                    var obj2 = points[k];

                    var diff = obj2.Child - obj1.Child;
                    var rotated = Vector2.Rotate(diff, Math.PI * 5 / 6);

                    newObjects.Add(new RelevantPoint(obj1.Child - (rotated / Math.Sqrt(3))));
                    newObjects.Add(new RelevantPoint(obj2.Child + (rotated / Math.Sqrt(3))));
                }
            }

            return newObjects;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantDrawable;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    class PerpendicularGenerator : RelevantObjectsGenerator, IGenerateLinesFromRelevantObjects {
        public override string Name => "Perpendicular Line Calculator";
        public override string Tooltip => "Takes a pair of line and point and generates a virtual line across the point that is perpendicular to the line.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        public List<IRelevantObject> GetRelevantObjects(List<IRelevantObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            for (int i = 0; i < objects.Count; i++) {
                for (int k = i + 1; k < objects.Count; k++) {
                    var obj1 = objects[i];
                    var obj2 = objects[k];

                    if (obj1 is RelevantLine line && obj2 is RelevantPoint point)
                    {
                        newObjects.Add(MakePerpendicularLine(line.Child, point.Child));
                    } else
                    if (obj2 is RelevantLine line2 && obj1 is RelevantPoint point2) {
                        newObjects.Add(MakePerpendicularLine(line2.Child, point2.Child));
                    }
                }
            }

            return newObjects;
        }

        private static RelevantLine MakePerpendicularLine(Line2 line, Vector2 point) {
            var perp = line.PerpendicularLeft();
            perp.PositionVector = point;
            return new RelevantLine(perp);
        }

        public List<RelevantLine> GetRelevantObjects(List<RelevantPoint> points, List<RelevantLine> lines, List<RelevantCircle> circles) {
            return (from line in lines from point in points select MakePerpendicularLine(line.Child, point.Child)).ToList();
        }
    }
}

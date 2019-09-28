using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.Layers {
    public class RelevantObjectLayer : ObjectLayer {
        public List<IRelevantObject> ObjectList {
            get {
                var objects = Lines.Concat<IRelevantObject>(Circles).Concat(Points).ToList();
                return objects;
            }
            set {
                Points = value.OfType<RelevantPoint>().ToList();
                Lines = value.OfType<RelevantLine>().ToList();
                Circles = value.OfType<RelevantCircle>().ToList();
            }
        }

        private List<RelevantPoint> Points = new List<RelevantPoint>();
        private List<RelevantLine> Lines = new List<RelevantLine>();
        private List<RelevantCircle> Circles = new List<RelevantCircle>();

        public void AddPoint(RelevantPoint point) {
            Points.Add(point);
        }

        public void AddPoints(List<RelevantPoint> points) {
            Points.AddRange(points);
        }

        /// <summary>
        /// Remove objects that are generated from concurrent generators
        /// </summary>
        public void DeleteObjectsFromConcurrent() {
            throw new NotImplementedException();
        }
    }
}

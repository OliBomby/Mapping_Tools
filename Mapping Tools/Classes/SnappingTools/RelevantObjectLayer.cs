using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SnappingTools {
    public class RelevantObjectLayer : ObjectLayer<IRelevantObject> {
        public new List<IRelevantObject> ObjectList {
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

        private List<RelevantPoint> Points;
        private List<RelevantLine> Lines;
        private List<RelevantCircle> Circles;
    }
}

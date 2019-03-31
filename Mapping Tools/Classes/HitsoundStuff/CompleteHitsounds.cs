using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class CompleteHitsounds {
        public List<Hitsound> Hitsounds;
        public List<CustomIndex> CustomIndices;

        public CompleteHitsounds(List<Hitsound> hitsounds, List<CustomIndex> customIndices) {
            Hitsounds = hitsounds;
            CustomIndices = customIndices;
        }

        public CompleteHitsounds(List<Hitsound> hitsounds) {
            Hitsounds = hitsounds;
            CustomIndices = new List<CustomIndex>();
        }

        public CompleteHitsounds(List<CustomIndex> customIndices) {
            Hitsounds = new List<Hitsound>();
            CustomIndices = customIndices;
        }

        public CompleteHitsounds() {
            Hitsounds = new List<Hitsound>();
            CustomIndices = new List<CustomIndex>();
        }
    }
}

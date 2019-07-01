using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class CompleteHitsounds {
        public List<HitsoundEvent> Hitsounds;
        public List<CustomIndex> CustomIndices;

        public CompleteHitsounds(List<HitsoundEvent> hitsounds, List<CustomIndex> customIndices) {
            Hitsounds = hitsounds;
            CustomIndices = customIndices;
        }

        public CompleteHitsounds(List<HitsoundEvent> hitsounds) {
            Hitsounds = hitsounds;
            CustomIndices = new List<CustomIndex>();
        }

        public CompleteHitsounds(List<CustomIndex> customIndices) {
            Hitsounds = new List<HitsoundEvent>();
            CustomIndices = customIndices;
        }

        public CompleteHitsounds() {
            Hitsounds = new List<HitsoundEvent>();
            CustomIndices = new List<CustomIndex>();
        }
    }
}

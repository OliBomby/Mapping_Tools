using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {

    /// <summary>
    /// 
    /// </summary>
    public class CompleteHitsounds {

        /// <summary>
        /// 
        /// </summary>
        public List<HitsoundEvent> Hitsounds;

        /// <summary>
        /// 
        /// </summary>
        public List<CustomIndex> CustomIndices;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitsounds"></param>
        /// <param name="customIndices"></param>
        public CompleteHitsounds(List<HitsoundEvent> hitsounds, List<CustomIndex> customIndices) {
            Hitsounds = hitsounds;
            CustomIndices = customIndices;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitsounds"></param>
        public CompleteHitsounds(List<HitsoundEvent> hitsounds) {
            Hitsounds = hitsounds;
            CustomIndices = new List<CustomIndex>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customIndices"></param>
        public CompleteHitsounds(List<CustomIndex> customIndices) {
            Hitsounds = new List<HitsoundEvent>();
            CustomIndices = customIndices;
        }

        /// <summary>
        /// 
        /// </summary>
        public CompleteHitsounds() {
            Hitsounds = new List<HitsoundEvent>();
            CustomIndices = new List<CustomIndex>();
        }
    }
}

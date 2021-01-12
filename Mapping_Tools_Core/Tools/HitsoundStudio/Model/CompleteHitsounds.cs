using System.Collections.Generic;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    /// <summary>
    /// Complete representation of custom hitsounds for osu! standard without sliderbody samples.
    /// </summary>
    public class CompleteHitsounds : ICompleteHitsounds {

        /// <summary>
        /// 
        /// </summary>
        public List<IHitsoundEvent> HitsoundEvents { get; }

        /// <summary>
        /// 
        /// </summary>
        public List<ICustomIndex> CustomIndices { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hitsoundEvents"></param>
        /// <param name="customIndices"></param>
        public CompleteHitsounds(List<IHitsoundEvent> hitsoundEvents, List<ICustomIndex> customIndices) {
            HitsoundEvents = hitsoundEvents;
            CustomIndices = customIndices;
        }
    }
}

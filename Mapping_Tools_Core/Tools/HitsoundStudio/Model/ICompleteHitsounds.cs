using System.Collections.Generic;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    public interface ICompleteHitsounds {
        List<IHitsoundEvent> HitsoundEvents { get; }
    }
}
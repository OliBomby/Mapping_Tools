using System.Collections.Generic;

namespace Mapping_Tools_Core.Tools.ComboColourStudio {
    public interface IColourPoint {
        double Time { get; }
        ColourPointMode Mode { get; }
        IReadOnlyList<int> ColourSequence { get; }
    }
}
using System.Collections.Generic;

namespace Mapping_Tools_Core.BeatmapHelper {
    public interface IComboColourCollection {
        IReadOnlyList<IComboColour> ComboColours { get; }
    }
}
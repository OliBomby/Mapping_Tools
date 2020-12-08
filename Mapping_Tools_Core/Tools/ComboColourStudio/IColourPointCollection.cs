using System.Collections.Generic;

namespace Mapping_Tools_Core.Tools.ComboColourStudio {
    public interface IColourPointCollection {
        IReadOnlyList<IColourPoint> ColourPoints { get; }
    }
}
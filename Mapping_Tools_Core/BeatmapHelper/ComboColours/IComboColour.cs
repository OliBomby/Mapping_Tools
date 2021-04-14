using System;

namespace Mapping_Tools_Core.BeatmapHelper.ComboColours {
    public interface IComboColour : ICloneable {
        byte R { get; }
        byte G { get; }
        byte B { get; }
    }
}
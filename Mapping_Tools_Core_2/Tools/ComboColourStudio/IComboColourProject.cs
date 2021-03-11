using Mapping_Tools_Core.BeatmapHelper;

namespace Mapping_Tools_Core.Tools.ComboColourStudio {
    public interface IComboColourProject : IComboColourCollection, IColourPointCollection {
        int MaxBurstLength { get; }
    }
}
using Mapping_Tools_Core.BeatmapHelper;
using System.Collections.Generic;

namespace Mapping_Tools_Core.Tools.ComboColourStudio {
    public class ComboColourProject : IComboColourProject {
        private readonly List<IColourPoint> _colourPoints;
        private readonly List<IComboColour> _comboColours;

        public IReadOnlyList<IColourPoint> ColourPoints => _colourPoints;

        public IReadOnlyList<IComboColour> ComboColours => _comboColours;

        public int MaxBurstLength { get; }

        public ComboColourProject(IEnumerable<IColourPoint> colourPoints, IEnumerable<IComboColour> comboColours, int maxBurstLength) {
            _colourPoints = new List<IColourPoint>(colourPoints);
            _comboColours = new List<IComboColour>(comboColours);
            MaxBurstLength = maxBurstLength;
        }
    }
}
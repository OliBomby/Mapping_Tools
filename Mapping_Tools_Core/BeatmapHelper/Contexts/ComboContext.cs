using Mapping_Tools_Core.BeatmapHelper.ComboColours;

namespace Mapping_Tools_Core.BeatmapHelper.Contexts {
    public class ComboContext : IContext {
        /// <summary>
        /// Whether a new combo starts on this hit object.
        /// </summary>
        public bool ActualNewCombo { get; set; }

        /// <summary>
        /// The combo number of this hit object.
        /// </summary>
        public int ComboIndex { get; set; }

        /// <summary>
        /// The colour index of the hit object.
        /// Determines which combo colour of the beatmap to use.
        /// </summary>
        public int ColourIndex { get; set; }

        /// <summary>
        /// The colour of this hit object.
        /// </summary>
        public IComboColour Colour { get; set; }

        public ComboContext(bool actualNewCombo, int comboIndex, int colourIndex, IComboColour colour) {
            ActualNewCombo = actualNewCombo;
            ComboIndex = comboIndex;
            ColourIndex = colourIndex;
            Colour = colour;
        }

        public IContext Copy() {
            return new ComboContext(ActualNewCombo, ComboIndex, ColourIndex, (IComboColour) Colour.Clone());
        }
    }
}
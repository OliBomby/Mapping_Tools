namespace Mapping_Tools.Domain.Beatmaps.Contexts;

public class ComboContext(bool actualNewCombo, int comboIndex, int colourIndex, ComboColour colour)
    : IContext {
    /// <summary>
    /// Whether a new combo starts on this hit object.
    /// </summary>
    public bool ActualNewCombo { get; set; } = actualNewCombo;

    /// <summary>
    /// The combo number of this hit object.
    /// </summary>
    public int ComboIndex { get; set; } = comboIndex;

    /// <summary>
    /// The colour index of the hit object.
    /// Determines which combo colour of the beatmap to use.
    /// </summary>
    public int ColourIndex { get; set; } = colourIndex;

    /// <summary>
    /// The colour of this hit object.
    /// </summary>
    public ComboColour Colour { get; set; } = colour;

    public IContext Copy() {
        return new ComboContext(ActualNewCombo, ComboIndex, ColourIndex, (ComboColour) Colour.Clone());
    }
}
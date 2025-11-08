namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14;

public class ComboColourEncoder : IEncoder<ComboColour> {
    public string Encode(ComboColour comboColour) {
        return $"{comboColour.R.ToInvariant()},{comboColour.G.ToInvariant()},{comboColour.B.ToInvariant()}";
    }
}
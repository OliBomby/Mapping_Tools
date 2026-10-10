using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;

namespace Mapping_Tools.Desktop.Tools.ComboColourStudio.Models;

/// <summary>Stores the persisted Combo Colour Studio project.</summary>
public sealed class ComboColourProject : ComboColourServiceOptions
{
    /// <summary>Creates an independent project snapshot for persistence or execution.</summary>
    /// <returns>A copied Desktop project retaining the current palette references.</returns>
    public new ComboColourProject Copy()
    {
        var source = base.Copy();
        ComboColourProject copy = new() { MaxBurstLength = source.MaxBurstLength };
        copy.ComboColours.AddRange(source.ComboColours.Select(colour => (SpecialColour)colour.Clone()));
        copy.ColourPoints.AddRange(source.ColourPoints.Select(point => (ColourPoint)point.Clone()));
        copy.MatchComboColourReferences();
        return copy;
    }
}

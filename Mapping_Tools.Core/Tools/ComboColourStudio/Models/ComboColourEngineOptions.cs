using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Tools.ComboColourStudio.Models;

/// <summary>
///     Serializable, framework-neutral Combo Colour Studio state.
/// </summary>
public class ComboColourEngineOptions
{
    /// <summary>Creates an empty project with the legacy burst-length default.</summary>
    public ComboColourEngineOptions()
    {
        MaxBurstLength = 1;
    }

    /// <summary>Gets or sets points in their current editing order.</summary>
    public List<ColourPoint> ColourPoints { get; set; } = [];

    /// <summary>Gets or sets the named palette, in editor order.</summary>
    public List<SpecialColour> ComboColours { get; set; } = [];

    /// <summary>Gets or sets the largest combo eligible for burst points.</summary>
    public int MaxBurstLength { get; set; }

    /// <summary>Adds a point using the supplied values and attaches it to this project.</summary>
    /// <param name="time">The offset in milliseconds.</param>
    /// <param name="colours">The initial ordered sequence.</param>
    /// <param name="mode">The point mode.</param>
    /// <returns>The new attached point.</returns>
    public ColourPoint AddColourPoint(
        double time = 0,
        IEnumerable<SpecialColour>? colours = null,
        ColourPointMode mode = ColourPointMode.Normal)
    {
        ColourPoint point = new(time, colours ?? [], mode);
        ColourPoints.Add(point);
        return point;
    }

    /// <summary>Removes the supplied points, or the last point when none are supplied.</summary>
    /// <param name="selectedPoints">The points selected by the presentation layer.</param>
    /// <returns>The number of points removed.</returns>
    public int RemoveSelectedOrLastColourPoints(IEnumerable<ColourPoint> selectedPoints)
    {
        ArgumentNullException.ThrowIfNull(selectedPoints);
        var selected = selectedPoints.Where(ColourPoints.Contains).Distinct().ToArray();
        if (selected.Length > 0)
        {
            foreach (var point in selected) ColourPoints.Remove(point);

            return selected.Length;
        }

        if (ColourPoints.Count == 0) return 0;

        ColourPoints.RemoveAt(ColourPoints.Count - 1);
        return 1;
    }

    /// <summary>Adds a named palette colour, copying the previous colour when available.</summary>
    /// <returns><see langword="true" /> when a colour was added; otherwise the eight-colour limit was reached.</returns>
    public bool AddComboColour()
    {
        if (ComboColours.Count >= 8) return false;

        var colour = ComboColours.Count == 0
            ? RgbaColour.White
            : ComboColours[^1].Color;
        ComboColours.Add(new SpecialColour(colour, $"Combo{ComboColours.Count + 1}"));
        return true;
    }

    /// <summary>Removes the last palette colour, preserving sequence entries for later reattachment.</summary>
    /// <returns><see langword="true" /> when a colour was removed.</returns>
    public bool RemoveLastComboColour()
    {
        if (ComboColours.Count == 0) return false;

        ComboColours.RemoveAt(ComboColours.Count - 1);
        return true;
    }

    /// <summary>Replaces sequence entries with the matching palette object by name.</summary>
    public void MatchComboColourReferences()
    {
        foreach (var point in ColourPoints)
            for (int index = 0; index < point.ColourSequence.Count; index++)
            {
                var current = point.ColourSequence[index];
                point.ColourSequence[index] = ComboColours.FirstOrDefault(colour => colour.Name == current.Name) ?? current;
            }
    }

    /// <summary>Creates a deep copy suitable for persistence or background execution.</summary>
    /// <returns>An independently mutable project copy.</returns>
    public ComboColourEngineOptions Copy()
    {
        ComboColourEngineOptions copy = new() { MaxBurstLength = MaxBurstLength };
        foreach (var colour in ComboColours) copy.ComboColours.Add((SpecialColour)colour.Clone());

        foreach (var point in ColourPoints) copy.ColourPoints.Add((ColourPoint)point.Clone());

        copy.MatchComboColourReferences();
        return copy;
    }
}

using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Tools.ComboColourStudio.Models;

/// <summary>Describes one non-mutating Combo Colour Studio preview entry.</summary>
public sealed record ComboColourPreviewEntry
{
    /// <summary>Creates a preview entry.</summary>
    /// <param name="time">The source point offset in milliseconds.</param>
    /// <param name="mode">The source point mode.</param>
    /// <param name="colourName">The named palette colour.</param>
    /// <param name="colour">The displayed RGBA value.</param>
    public ComboColourPreviewEntry(double time, ColourPointMode mode, string colourName, RgbaColour colour)
    {
        Time = time;
        Mode = mode;
        ColourName = colourName;
        Colour = colour;
    }

    /// <summary>Gets the source point offset in milliseconds.</summary>
    public double Time { get; }

    /// <summary>Gets the source point mode.</summary>
    public ColourPointMode Mode { get; }

    /// <summary>Gets the palette name.</summary>
    public string ColourName { get; }

    /// <summary>Gets the preview RGBA value.</summary>
    public RgbaColour Colour { get; }
}

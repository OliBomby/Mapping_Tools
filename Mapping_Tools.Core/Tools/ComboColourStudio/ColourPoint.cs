using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <summary>
/// Associates a beatmap offset with an ordered combo-colour sequence.
/// </summary>
public sealed class ColourPoint : ICloneable
{
    /// <summary>Creates an empty normal point at offset zero.</summary>
    public ColourPoint() : this(0, [], ColourPointMode.Normal)
    {
    }

    /// <summary>Creates a point with the supplied offset, sequence, and mode.</summary>
    /// <param name="time">The offset in milliseconds.</param>
    /// <param name="colourSequence">The ordered combo-colour references.</param>
    /// <param name="mode">The point application mode.</param>
    public ColourPoint(
        double time,
        IEnumerable<SpecialColour> colourSequence,
        ColourPointMode mode)
    {
        ArgumentNullException.ThrowIfNull(colourSequence);
        Time = time;
        ColourSequence = colourSequence.ToList();
        Mode = mode;
    }

    /// <summary>Gets or sets the point offset in milliseconds.</summary>
    public double Time { get; set; }

    /// <summary>Gets or sets the ordered colours used by this point.</summary>
    public List<SpecialColour> ColourSequence { get; set; }

    /// <summary>Gets or sets whether this point is normal or one-combo burst mode.</summary>
    public ColourPointMode Mode { get; set; }

    /// <summary>Creates an independent point copy, including sequence entries.</summary>
    /// <returns>A detached copy with equivalent persisted values.</returns>
    public object Clone() => new ColourPoint(
        Time,
        ColourSequence.Select(colour => (SpecialColour)colour.Clone()),
        Mode);
}

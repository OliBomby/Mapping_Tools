namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <summary>
/// All colour point modes.
/// </summary>
public enum ColourPointMode {
    /// <summary>
    /// Default mode which continuously cycles the colour sequence of the colour point until a new colour point comes.
    /// </summary>
    Normal,
    /// <summary>
    /// Special mode which uses the first colour of the colour sequence for only one combo and then continues with the previous colour point.
    /// Colour points with this mode may be ignored if the length of the combo is over a certain threshold.
    /// </summary>
    Burst
}
using System.Collections.Generic;

namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <summary>
/// Control point for custom colour combo colours.
/// </summary>
public interface IColourPoint {
    /// <summary>
    /// The absolute time in milliseconds.
    /// </summary>
    double Time { get; }

    /// <summary>
    /// The mode.
    /// </summary>
    ColourPointMode Mode { get; }

    /// <summary>
    /// The sequence of colour indices.
    /// </summary>
    IReadOnlyList<int> ColourSequence { get; }
}
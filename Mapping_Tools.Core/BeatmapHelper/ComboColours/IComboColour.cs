using System;

namespace Mapping_Tools.Core.BeatmapHelper.ComboColours;

/// <summary>
/// RGB colour.
/// </summary>
public interface IComboColour : ICloneable, IEquatable<IComboColour> {
    /// <summary>
    /// The red component of the colour.
    /// </summary>
    byte R { get; }

    /// <summary>
    /// The green component of the colour.
    /// </summary>
    byte G { get; }

    /// <summary>
    /// The blue component of the colour.
    /// </summary>
    byte B { get; }
}
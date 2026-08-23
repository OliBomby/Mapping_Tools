namespace Mapping_Tools.Core.BeatmapHelper;

/// <summary>
///     Associates a named osu! special-colour key, such as slider border, with an RGBA value.
/// </summary>
public class SpecialColour : ComboColour, IEquatable<SpecialColour>, ICloneable
{
    /// <summary>
    ///     Creates an unnamed colour with the base class default value.
    /// </summary>
    public SpecialColour() { }

    /// <summary>
    ///     Creates an unnamed special colour.
    /// </summary>
    /// <param name="color">The RGBA value.</param>
    public SpecialColour(RgbaColour color) : base(color) { }

    /// <summary>
    ///     Creates a named special colour.
    /// </summary>
    /// <param name="color">The RGBA value.</param>
    /// <param name="name">The <c>[Colours]</c> section key.</param>
    public SpecialColour(RgbaColour color, string name) : base(color)
    {
        Name = name;
    }

    /// <summary>
    ///     Gets or sets the key used in the beatmap <c>[Colours]</c> section.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     Copies both the colour value and section key into a new instance.
    /// </summary>
    /// <returns>An independently mutable special colour.</returns>
    public object Clone()
    {
        return new SpecialColour(Color, Name ?? string.Empty);
    }

    /// <summary>
    ///     Compares both the section key and RGBA value.
    /// </summary>
    /// <param name="other">The special colour to compare.</param>
    /// <returns><see langword="true" /> when name and colour both match.</returns>
    public bool Equals(SpecialColour? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && Color == other.Color;
    }

    /// <summary>
    ///     Determines whether an object is the same special-colour runtime type and value.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when it has the same name and RGBA value.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((SpecialColour)obj);
    }

    /// <summary>
    ///     Returns a hash based on the section key.
    /// </summary>
    /// <returns>The key hash, or zero for an unnamed colour.</returns>
    public override int GetHashCode()
    {
        return Name?.GetHashCode() ?? 0;
    }
}

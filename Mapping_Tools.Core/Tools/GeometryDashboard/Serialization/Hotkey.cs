namespace Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;

/// <summary>
///     Stores a keyboard key and modifier bit mask without depending on a frontend
///     input library. The numeric values intentionally match WPF's legacy values.
/// </summary>
public sealed class Hotkey : ICloneable, IEquatable<Hotkey>
{
    /// <summary>Creates a persisted hotkey from its legacy numeric values.</summary>
    /// <param name="key">The WPF-compatible key enum value; zero disables the key.</param>
    /// <param name="modifiers">The WPF-compatible modifier bit mask.</param>
    public Hotkey(int key = 0, int modifiers = 0)
    {
        Key = key;
        Modifiers = modifiers;
    }

    /// <summary>Gets or sets the WPF-compatible key enum value.</summary>
    public int Key { get; set; }

    /// <summary>Gets or sets the WPF-compatible modifier bit mask.</summary>
    public int Modifiers { get; set; }

    /// <inheritdoc />
    public object Clone()
    {
        return new Hotkey(Key, Modifiers);
    }

    /// <inheritdoc />
    public bool Equals(Hotkey? other)
    {
        return other is not null && Key == other.Key && Modifiers == other.Modifiers;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Hotkey other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return unchecked(Key * 397 ^ Modifiers);
    }
}

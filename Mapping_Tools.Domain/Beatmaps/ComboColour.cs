namespace Mapping_Tools.Domain.Beatmaps;

/// <summary>
/// Combo colour or special color value type used in osu! beatmaps.
/// The british alternative because main developer wants to keep the spelling.
/// Its spelled "Colours" in the game.
/// </summary>
public class ComboColour {
    /// <summary>
    /// The red component value of the colour.
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// The green component value of the colour.
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// The blue component value of the colour.
    /// </summary>
    public byte B { get; }

    /// <summary>
    /// Constructs a new combo colour with provided red, green, and blue components.
    /// </summary>
    /// <param name="r">The red component</param>
    /// <param name="g">The green component</param>
    /// <param name="b">The blue component</param>
    public ComboColour(byte r, byte g, byte b) {
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// Constructs a new combo colour with provided red, green, and blue components as ints.
    /// These ints get casted to bytes.
    /// </summary>
    /// <param name="r">The red component</param>
    /// <param name="g">The green component</param>
    /// <param name="b">The blue component</param>
    public ComboColour(int r, int g, int b) : this((byte)r, (byte) g, (byte) b) { }

    public object Clone() {
        return MemberwiseClone();
    }

    public bool Equals(ComboColour? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return R == other.R && G == other.G && B == other.B;
    }

    ///<inheritdoc/>
    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((ComboColour) obj);
    }

    ///<inheritdoc/>
    public override int GetHashCode() {
        unchecked {
            var hashCode = R.GetHashCode();
            hashCode = hashCode * 397 ^ G.GetHashCode();
            hashCode = hashCode * 397 ^ B.GetHashCode();
            return hashCode;
        }
    }
}
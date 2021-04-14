using System;
using Mapping_Tools_Core.Exceptions;

namespace Mapping_Tools_Core.BeatmapHelper.ComboColours {
    /// <summary>
    /// The british alternative because main developer wants to keep the spelling.
    /// Its spelled "Colours" in the game.
    /// </summary>
    public class ComboColour : IComboColour, IEquatable<ComboColour> {
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

        /// <summary>
        /// Constructs a new combo colour from the provided line of .osu code.
        /// </summary>
        /// <param name="line">The line of .osu code</param>
        public ComboColour(string line) {
            string[] split = line.Split(':');
            string[] commaSplit = split[1].Split(',');

            if (!InputParsers.TryParseInt(commaSplit[0], out int r))
                throw new BeatmapParsingException("Failed to parse red component of colour.", line);

            if (!InputParsers.TryParseInt(commaSplit[1], out int g))
                throw new BeatmapParsingException("Failed to parse green component of colour.", line);

            if (!InputParsers.TryParseInt(commaSplit[2], out int b))
                throw new BeatmapParsingException("Failed to parse blue component of colour.", line);

            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
        }

        /// <summary>
        /// Converts an <see cref="IComboColour"/> to .osu code.
        /// </summary>
        /// <param name="comboColour">The combo colour to serialize</param>
        /// <returns></returns>
        public static string SerializeComboColour(IComboColour comboColour) {
            return $"{comboColour.R.ToInvariant()},{comboColour.G.ToInvariant()},{comboColour.B.ToInvariant()}";
        }

        public object Clone() {
            return MemberwiseClone();
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() {
            return SerializeComboColour(this);
        }

        /// <summary>
        /// Returns the 4 default combo colours of osu!
        /// </summary>
        /// <returns></returns>
        public static ComboColour[] GetDefaultComboColours() {
            return new []{new ComboColour(255, 192, 0),
                new ComboColour(0, 202, 0),
                new ComboColour(18, 124, 255),
                new ComboColour(242, 24, 57)};
        }

        public bool Equals(ComboColour other) {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ComboColour) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = R.GetHashCode();
                hashCode = (hashCode * 397) ^ G.GetHashCode();
                hashCode = (hashCode * 397) ^ B.GetHashCode();
                return hashCode;
            }
        }
    }
}

using System;

namespace Mapping_Tools_Core.Audio.Midi {
    /// <summary>
    /// Describes a full MIDI note sound, including additional channel effects.
    /// A value of -1 in any property is to be interpreted as "I don't care, just do anything."
    /// </summary>
    public interface IMidiNote : IEquatable<IMidiNote>, ICloneable {
        /// <summary>
        /// Channel bank number.
        /// Ranges from 0-127.
        /// </summary>
        int Bank { get; }

        /// <summary>
        /// Channel patch number.
        /// Ranges from 0-127.
        /// </summary>
        int Patch { get; }

        /// <summary>
        /// MIDI key number.
        /// Ranges from 0-127.
        /// </summary>
        int Key { get; }

        /// <summary>
        /// MIDI velocity number.
        /// Ranges from 0-127.
        /// </summary>
        int Velocity { get; }

        /// <summary>
        /// Note duration in seconds.
        /// </summary>
        double Length { get; }
    }
}
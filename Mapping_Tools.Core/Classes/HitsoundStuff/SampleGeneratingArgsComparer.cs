using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// Compares sample specifications according to whether generated-file identity or complete source identity matters.
    /// </summary>
    public class SampleGeneratingArgsComparer : IEqualityComparer<SampleGeneratingArgs> {
        /// <summary>
        /// Controls whether ordinary audio files ignore SoundFont-only selectors and SoundFonts ignore gain.
        /// </summary>
        public bool UseSampleFile { get; set; }

        /// <summary>
        /// Creates a comparer for generated-file or complete-configuration identity.
        /// </summary>
        /// <param name="useSampleFile">Whether equality should follow the fields that affect the generated sample file.</param>
        public SampleGeneratingArgsComparer(bool useSampleFile = true) {
            UseSampleFile = useSampleFile;
        }

        /// <summary>
        /// Compares paths and relevant rendering fields, using numeric tolerance for doubles.
        /// </summary>
        /// <param name="x">The first specification.</param>
        /// <param name="y">The second specification.</param>
        /// <returns><see langword="true"/> when both resolve to the same identity under the configured policy.</returns>
        public bool Equals(SampleGeneratingArgs x, SampleGeneratingArgs y) {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;

            if (UseSampleFile) {
                if (x.GetExtension().ToLower() == ".sf2" && y.GetExtension().ToLower() == ".sf2") {
                    return x.Path == y.Path &&
                           x.Bank == y.Bank &&
                           x.Patch == y.Patch &&
                           x.Instrument == y.Instrument &&
                           x.Key == y.Key &&
                           Precision.AlmostEquals(x.Length, y.Length) &&
                           x.Velocity == y.Velocity &&
                           Precision.AlmostEquals(x.Panning, y.Panning) &&
                           Precision.AlmostEquals(x.PitchShift, y.PitchShift);
                }

                return x.Path == y.Path &&
                       Precision.AlmostEquals(x.Volume, y.Volume) &&
                       Precision.AlmostEquals(x.Panning, y.Panning) &&
                       Precision.AlmostEquals(x.PitchShift, y.PitchShift);
            }

            return x.Path == y.Path &&
                   Precision.AlmostEquals(x.Volume, y.Volume) &&
                   Precision.AlmostEquals(x.Panning, y.Panning) &&
                   Precision.AlmostEquals(x.PitchShift, y.PitchShift) &&
                   x.Bank == y.Bank &&
                   x.Patch == y.Patch &&
                   x.Instrument == y.Instrument &&
                   x.Key == y.Key &&
                   Precision.AlmostEquals(x.Length, y.Length);
        }

        /// <summary>
        /// Hashes the fields selected by <see cref="UseSampleFile"/>.
        /// </summary>
        /// <param name="obj">The specification to hash.</param>
        /// <returns>A hash code for the configured equality policy.</returns>
        public int GetHashCode(SampleGeneratingArgs obj) {
            var hashCode = 0x34894079;
            hashCode = hashCode * -0x5AAAAAD7 + EqualityComparer<string>.Default.GetHashCode(obj.Path);
            hashCode = hashCode * -0x5AAAAAD7 + obj.Volume.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + obj.Panning.GetHashCode();
            hashCode = hashCode * -0x5AAAAAD7 + obj.PitchShift.GetHashCode();
            if (!UseSampleFile || obj.GetExtension().ToLower() == ".sf2") {
                hashCode = hashCode * -0x5AAAAAD7 + obj.Bank.GetHashCode();
                hashCode = hashCode * -0x5AAAAAD7 + obj.Patch.GetHashCode();
                hashCode = hashCode * -0x5AAAAAD7 + obj.Instrument.GetHashCode();
                hashCode = hashCode * -0x5AAAAAD7 + obj.Key.GetHashCode();
                hashCode = hashCode * -0x5AAAAAD7 + obj.Length.GetHashCode();
            }
            return hashCode;
        }
    }
}

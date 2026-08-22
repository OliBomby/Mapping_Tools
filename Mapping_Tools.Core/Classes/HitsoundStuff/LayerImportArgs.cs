using Mapping_Tools.Core.Classes.MathUtil;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Mapping_Tools.Core.Classes.HitsoundStuff {
    /// <summary>
    /// Holds filters and sample-generation settings for one hitsound import layer.
    /// </summary>
    public class LayerImportArgs : IEquatable<LayerImportArgs> {
        /// <inheritdoc />
        public LayerImportArgs() { }

        /// <inheritdoc />
        public LayerImportArgs(ImportType importType) => ImportType = importType;

        /// <summary>
        /// Gets or sets how the source is interpreted.
        /// </summary>
        public ImportType ImportType { get; set; }

        /// <summary>
        /// Gets or sets the imported beatmap, stack, MIDI, or hitsound source path.
        /// </summary>
        public string Path { get; set; } = "";

        /// <summary>
        /// Gets or sets the stack X filter, or -1 to accept any X coordinate.
        /// </summary>
        public double X { get; set; } = -1;

        /// <summary>
        /// Gets or sets the stack Y filter, or -1 to accept any Y coordinate.
        /// </summary>
        public double Y { get; set; } = -1;

        /// <summary>
        /// Gets or sets the audio file or SoundFont used to render imported events.
        /// </summary>
        public string SamplePath { get; set; } = "";

        /// <summary>
        /// Gets or sets the linear sample gain from which <see cref="Velocity"/> is derived.
        /// </summary>
        public double Volume { get; set; } = -1d / 127;

        /// <summary>
        /// Controls whether otherwise matching hitsound imports remain separate when their volumes differ.
        /// </summary>
        public bool DiscriminateVolumes { get; set; }

        /// <summary>
        /// Controls whether imported audio content is compared to detect duplicate files.
        /// </summary>
        public bool DetectDuplicateSamples { get; set; }

        /// <summary>
        /// Controls whether detected duplicate import events are removed.
        /// </summary>
        public bool RemoveDuplicates { get; set; }

        /// <summary>
        /// Gets or sets the SoundFont bank, or -1 to accept any bank.
        /// </summary>
        public int Bank { get; set; } = -1;

        /// <summary>
        /// Gets or sets the SoundFont patch, or -1 to accept any patch.
        /// </summary>
        public int Patch { get; set; } = -1;

        /// <summary>
        /// Gets or sets the MIDI key, or -1 to accept any note.
        /// </summary>
        public int Key { get; set; } = -1;

        /// <summary>
        /// Gets or sets the MIDI note length, or -1 to accept any length.
        /// </summary>
        public double Length { get; set; } = -1;

        /// <summary>
        /// Gets or sets the tolerance used when grouping MIDI note lengths.
        /// </summary>
        public double LengthRoughness { get; set; } = 1;

        /// <summary>
        /// Gets or sets MIDI velocity through the linear <see cref="Volume"/> scale.
        /// </summary>
        public int Velocity {
            get => (int)Math.Round(Volume * 127);
            set => Volume = Velocity == value ? Volume : value / 127d;
        }

        /// <summary>
        /// Gets or sets the tolerance used when grouping MIDI velocities.
        /// </summary>
        public double VelocityRoughness { get; set; } = 1;

        /// <summary>
        /// Gets or sets the millisecond offset applied to imported events.
        /// </summary>
        public double Offset { get; set; }


        /// <summary>
        /// Captures the subset of settings that controls source-data cache invalidation.
        /// </summary>
        /// <returns></returns>
        public ImportReloadingArgs GetImportReloadingArgs() {
            return new ImportReloadingArgs(ImportType, Path, X, Y, LengthRoughness, VelocityRoughness, DiscriminateVolumes, DetectDuplicateSamples, RemoveDuplicates, Offset);
        }

        /// <summary>
        /// Determines whether cached imported data can be reused for another layer configuration, honoring wildcard filters.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public bool ReloadCompatible(LayerImportArgs o) {
            if (ImportType != o.ImportType)
                return false;

            switch (ImportType) {
                case ImportType.Stack:
                    return Path == o.Path && (X == -1 || X == o.X) && (Y == -1 || Y == o.Y);
                case ImportType.Hitsounds:
                    return Path == o.Path && SamplePath == o.SamplePath && (!DiscriminateVolumes || Math.Abs(Volume - o.Volume) < Precision.DoubleEpsilon);
                case ImportType.MIDI:
                    return Path == o.Path && (Bank == -1 || Bank == o.Bank) && (Patch == -1 || Patch == o.Patch) && (Key == -1 || Key == o.Key)
                                          && (Length == -1 || Length == o.Length) && (Velocity == -1 || Velocity == o.Velocity);
                case ImportType.Storyboard:
                    return Path == o.Path && SamplePath == o.SamplePath && (!DiscriminateVolumes || Math.Abs(Volume - o.Volume) < Precision.DoubleEpsilon);
                case ImportType.None:
                    return true;
                default:
                    return Equals(o);
            }
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(LayerImportArgs other) {
            return Path == other.Path &&
                ImportType == other.ImportType &&
                X == other.X &&
                Y == other.Y &&
                SamplePath == other.SamplePath &&
                Bank == other.Bank &&
                Patch == other.Patch &&
                Key == other.Key &&
                Length == other.Length &&
                LengthRoughness == other.LengthRoughness &&
                Velocity == other.Velocity &&
                VelocityRoughness == other.VelocityRoughness &&
                RemoveDuplicates == other.RemoveDuplicates &&
                DiscriminateVolumes == other.DiscriminateVolumes &&
                DetectDuplicateSamples == other.DetectDuplicateSamples &&
                Offset == other.Offset;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (!(obj is LayerImportArgs)) {
                return false;
            }

            return Equals((LayerImportArgs)obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() {
            var hashCode = -421944398;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Path);
            hashCode = hashCode * -1521134295 + ImportType.GetHashCode();
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SamplePath);
            hashCode = hashCode * -1521134295 + Bank.GetHashCode();
            hashCode = hashCode * -1521134295 + Patch.GetHashCode();
            hashCode = hashCode * -1521134295 + Key.GetHashCode();
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            hashCode = hashCode * -1521134295 + LengthRoughness.GetHashCode();
            hashCode = hashCode * -1521134295 + Velocity.GetHashCode();
            hashCode = hashCode * -1521134295 + VelocityRoughness.GetHashCode();
            hashCode = hashCode * -1521134295 + RemoveDuplicates.GetHashCode();
            hashCode = hashCode * -1521134295 + DiscriminateVolumes.GetHashCode();
            hashCode = hashCode * -1521134295 + DetectDuplicateSamples.GetHashCode();
            hashCode = hashCode * -1521134295 + Offset.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Applies the == operator.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true"/> when all import and sample-generation settings match.</returns>
        public static bool operator ==(LayerImportArgs left, object right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Applies the != operator.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns><see langword="true"/> when any import or sample-generation setting differs.</returns>
        public static bool operator !=(LayerImportArgs left, object right) {
            return !left.Equals(right);
        }
    }
}

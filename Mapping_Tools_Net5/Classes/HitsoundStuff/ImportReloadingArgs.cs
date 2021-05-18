using System;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// 
    /// </summary>
    public class ImportReloadingArgs : IEquatable<ImportReloadingArgs> {
        /// <summary>
        /// 
        /// </summary>
        public ImportType ImportType { get; }
        public string Path { get; }
        public double X { get; }
        public double Y { get; }
        public double LengthRoughness { get; }
        public double VelocityRoughness { get; }
        public bool DiscriminateVolumes { get; }
        public bool DetectDuplicateSamples { get; }
        public bool RemoveDuplicates { get; }

        /// <inheritdoc />
        public ImportReloadingArgs(string path) : this(ImportType.None, path, -1, -1, -1, -1, false, false, false) {
        }

        /// <inheritdoc />
        public ImportReloadingArgs(ImportType importType, string path, double x, double y, double lengthRoughness, double velocityRoughness,
            bool discriminateVolumes, bool detectDuplicateSamples, bool removeDuplicates) {
            ImportType = importType;
            Path = path;
            X = x;
            Y = y;
            LengthRoughness = lengthRoughness;
            VelocityRoughness = velocityRoughness;
            DiscriminateVolumes = discriminateVolumes;
            DetectDuplicateSamples = detectDuplicateSamples;
            RemoveDuplicates = removeDuplicates;
        }

        /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
        public bool Equals(ImportReloadingArgs other) {
            return Path == other.Path &&
                ImportType == other.ImportType &&
                X == other.X &&
                Y == other.Y &&
                LengthRoughness == other.LengthRoughness &&
                VelocityRoughness == other.VelocityRoughness &&
                DiscriminateVolumes == other.DiscriminateVolumes &&
                DetectDuplicateSamples == other.DetectDuplicateSamples &&
                RemoveDuplicates == other.RemoveDuplicates;
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object. </param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj) {
            if (!(obj is ImportReloadingArgs)) {
                return false;
            }

            return Equals((ImportReloadingArgs)obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() {
            var hashCode = 1887348610;
            hashCode = hashCode * -1521134295 + ImportType.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Path);
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + LengthRoughness.GetHashCode();
            hashCode = hashCode * -1521134295 + VelocityRoughness.GetHashCode();
            hashCode = hashCode * -1521134295 + RemoveDuplicates.GetHashCode();
            hashCode = hashCode * -1521134295 + DiscriminateVolumes.GetHashCode();
            hashCode = hashCode * -1521134295 + DetectDuplicateSamples.GetHashCode();
            return hashCode;
        }
    }
}

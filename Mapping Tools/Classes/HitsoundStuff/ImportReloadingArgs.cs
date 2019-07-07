using System;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class ImportReloadingArgs : IEquatable<ImportReloadingArgs> {
        public ImportType ImportType { get; set; }
        public string Path { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double LengthRoughness { get; set; }
        public double VelocityRoughness { get; set; }

        public ImportReloadingArgs(string path) {
            Path = path;
            LengthRoughness = -1;
            VelocityRoughness = -1;
            X = -1;
            Y = -1;
        }

        public ImportReloadingArgs(ImportType importType, string path, double x, double y, double lengthRoughness, double velocityRoughness) {
            ImportType = importType;
            Path = path;
            X = x;
            Y = y;
            LengthRoughness = lengthRoughness;
            VelocityRoughness = velocityRoughness;
        }

        public bool Equals(ImportReloadingArgs other) {
            return Path == other.Path &&
                ImportType == other.ImportType &&
                X == other.X &&
                Y == other.Y &&
                LengthRoughness == other.LengthRoughness &&
                VelocityRoughness == other.VelocityRoughness;
        }

        public override bool Equals(object obj) {
            if (!(obj is ImportReloadingArgs)) {
                return false;
            }

            return Equals((ImportReloadingArgs)obj);
        }

        public override int GetHashCode() {
            var hashCode = 1887348610;
            hashCode = hashCode * -1521134295 + ImportType.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Path);
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + LengthRoughness.GetHashCode();
            hashCode = hashCode * -1521134295 + VelocityRoughness.GetHashCode();
            return hashCode;
        }
    }
}

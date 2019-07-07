using System.Collections.Generic;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class ImportReloadingArgsComparer : IEqualityComparer<ImportReloadingArgs> {
        public bool Equals(ImportReloadingArgs x, ImportReloadingArgs y) {
            if (x.ImportType != y.ImportType)
                return false;

            switch (x.ImportType) {
                case ImportType.Stack:
                    return x.Path == y.Path &&
                    x.X == y.X &&
                    x.Y == y.Y;
                case ImportType.Hitsounds:
                    return x.Path == y.Path;
                case ImportType.MIDI:
                    return x.Path == y.Path &&
                    x.LengthRoughness == y.LengthRoughness &&
                    x.VelocityRoughness == y.VelocityRoughness;
                case ImportType.None:
                    return true;
                default:
                    return x.Equals(y);
            }
        }

        public int GetHashCode(ImportReloadingArgs x) {
            var hashCode = 1887348610;
            hashCode = hashCode * -1521134295 + x.ImportType.GetHashCode();
            switch (x.ImportType) {
                case ImportType.Stack:
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(x.Path);
                    hashCode = hashCode * -1521134295 + x.X.GetHashCode();
                    hashCode = hashCode * -1521134295 + x.Y.GetHashCode();
                    return hashCode;
                case ImportType.Hitsounds:
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(x.Path);
                    return hashCode;
                case ImportType.MIDI:
                    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(x.Path);
                    hashCode = hashCode * -1521134295 + x.LengthRoughness.GetHashCode();
                    hashCode = hashCode * -1521134295 + x.VelocityRoughness.GetHashCode();
                    return hashCode;
                case ImportType.None:
                    return hashCode;
                default:
                    return x.GetHashCode();
            }
        }
    }
}

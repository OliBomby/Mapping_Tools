using System.Collections.Generic;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class SampleGeneratingArgsComparer : IEqualityComparer<SampleGeneratingArgs> {
        public bool UseSampleFile { get; set; }

        public SampleGeneratingArgsComparer(bool useSampleFile = true) {
            UseSampleFile = useSampleFile;
        }

        public bool Equals(SampleGeneratingArgs x, SampleGeneratingArgs y) {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;

            if (UseSampleFile) {
                if (x.GetExtension() == ".sf2" && y.GetExtension() == ".sf2") {
                    return x.Path == y.Path &&
                           x.Bank == y.Bank &&
                           x.Patch == y.Patch &&
                           x.Instrument == y.Instrument &&
                           x.Key == y.Key &&
                           x.Length == y.Length &&
                           x.Velocity == y.Velocity;
                }

                return x.Path == y.Path &&
                       x.Volume == y.Volume;
            }

            return x.Path == y.Path &&
                   x.Volume == y.Volume &&
                   x.Bank == y.Bank &&
                   x.Patch == y.Patch &&
                   x.Instrument == y.Instrument &&
                   x.Key == y.Key &&
                   x.Length == y.Length;
        }

        public int GetHashCode(SampleGeneratingArgs obj) {
            var hashCode = 0x34894079;
            hashCode = hashCode * -0x5AAAAAD7 + EqualityComparer<string>.Default.GetHashCode(obj.Path);
            hashCode = hashCode * -0x5AAAAAD7 + obj.Volume.GetHashCode();
            if (!UseSampleFile || obj.GetExtension() == ".sf2") {
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

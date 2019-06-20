using NAudio.SoundFont;

namespace Mapping_Tools.Classes.HitsoundStuff {
    static class SF2Extension {
        public static Instrument Instrument(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.Instrument);
            return g?.Instrument;
        }

        public static ushort KeyRange(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.KeyRange);
            return g != null ? g.UInt16Amount : (ushort)0;
        }

        public static ushort VelocityRange(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.VelocityRange);
            return g != null ? g.UInt16Amount : (ushort)0;
        }

        public static byte Velocity(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.Velocity);
            return g != null ? g.LowByteAmount : (byte)127;
        }

        public static byte OverridingRootKey(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.OverridingRootKey);
            return g != null ? g.LowByteAmount : (byte)0;
        }

        public static byte Key(this Zone zone) {
            var sh = zone.SampleHeader();
            if (sh == null)
                return 0;

            byte over = zone.OverridingRootKey();
            return over != 0 ? over : sh.OriginalPitch;
        }

        public static int SampleModes(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.SampleModes);
            return g != null ? g.UInt16Amount : (ushort)0;
        }

        public static SampleHeader SampleHeader(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.SampleID);
            return g?.SampleHeader;
        }

        public static Generator SelectByGenerator(this Zone zone, GeneratorEnum type) {
            foreach (var g in zone.Generators)
                if (g.GeneratorType == type)
                    return g;
            return null;
        }
    }
}

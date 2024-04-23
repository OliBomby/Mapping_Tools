using NAudio.SoundFont;

namespace Mapping_Tools.Classes.HitsoundStuff {
    static class Sf2Extension {
        public static Instrument Instrument(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.Instrument);
            return g?.Instrument;
        }

        public static short StartAddressOffset(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.StartAddressOffset);
            return g?.Int16Amount ?? 0;
        }

        public static short StartAddressCoarseOffset(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.StartAddressCoarseOffset);
            return g?.Int16Amount ?? 0;
        }

        public static int FullStartAddressOffset(this Zone zone) {
            var g = StartAddressOffset(zone);
            var gc = StartAddressCoarseOffset(zone);
            return g + 0x8000 * gc;
        }

        public static short EndAddressOffset(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.EndAddressOffset);
            return g?.Int16Amount ?? 0;
        }

        public static short EndAddressCoarseOffset(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.EndAddressCoarseOffset);
            return g?.Int16Amount ?? 0;
        }

        public static int FullEndAddressOffset(this Zone zone) {
            var g = EndAddressOffset(zone);
            var gc = EndAddressCoarseOffset(zone);
            return g + 0x8000 * gc;
        }

        public static short StartLoopAddressOffset(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.StartLoopAddressOffset);
            return g?.Int16Amount ?? 0;
        }

        public static short StartLoopAddressCoarseOffset(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.StartLoopAddressCoarseOffset);
            return g?.Int16Amount ?? 0;
        }

        public static int FullStartLoopAddressOffset(this Zone zone) {
            var g = StartLoopAddressOffset(zone);
            var gc = StartLoopAddressCoarseOffset(zone);
            return g + 0x8000 * gc;
        }

        public static short EndLoopAddressOffset(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.EndLoopAddressOffset);
            return g?.Int16Amount ?? 0;
        }

        public static short EndLoopAddressCoarseOffset(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.EndLoopAddressCoarseOffset);
            return g?.Int16Amount ?? 0;
        }

        public static int FullEndLoopAddressOffset(this Zone zone) {
            var g = EndLoopAddressOffset(zone);
            var gc = EndLoopAddressCoarseOffset(zone);
            return g + 0x8000 * gc;
        }

        public static ushort KeyRange(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.KeyRange);
            return g?.UInt16Amount ?? 0;
        }

        public static ushort VelocityRange(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.VelocityRange);
            return g?.UInt16Amount ?? 0;
        }

        public static byte Velocity(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.Velocity);
            return g?.LowByteAmount ?? 127;
        }

        public static byte OverridingRootKey(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.OverridingRootKey);
            return g?.LowByteAmount ?? 0;
        }

        public static double Pan(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.Pan);
            return g?.Int16Amount / 500d ?? 0;
        }

        public static double Attenuation(this Zone zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.InitialAttenuation);
            return g?.Int16Amount / 10d ?? 0;
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
            return g?.UInt16Amount ?? 0;
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

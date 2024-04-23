using NAudio.SoundFont;

namespace Mapping_Tools.Classes.HitsoundStuff {
    static internal class Sf2Extension {
        public static Instrument Instrument(this Zone zone) {
            return zone.Generators.Instrument();
        }

        public static Instrument Instrument(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.Instrument);
            return g?.Instrument;
        }

        public static short StartAddressOffset(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.StartAddressOffset);
            return g?.Int16Amount ?? 0;
        }

        public static short StartAddressCoarseOffset(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.StartAddressCoarseOffset);
            return g?.Int16Amount ?? 0;
        }

        public static int FullStartAddressOffset(this Generator[] zone) {
            var g = StartAddressOffset(zone);
            var gc = StartAddressCoarseOffset(zone);
            return g + 0x8000 * gc;
        }

        public static short EndAddressOffset(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.EndAddressOffset);
            return g?.Int16Amount ?? 0;
        }

        public static short EndAddressCoarseOffset(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.EndAddressCoarseOffset);
            return g?.Int16Amount ?? 0;
        }

        public static int FullEndAddressOffset(this Generator[] zone) {
            var g = EndAddressOffset(zone);
            var gc = EndAddressCoarseOffset(zone);
            return g + 0x8000 * gc;
        }

        public static short StartLoopAddressOffset(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.StartLoopAddressOffset);
            return g?.Int16Amount ?? 0;
        }

        public static short StartLoopAddressCoarseOffset(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.StartLoopAddressCoarseOffset);
            return g?.Int16Amount ?? 0;
        }

        public static int FullStartLoopAddressOffset(this Generator[] zone) {
            var g = StartLoopAddressOffset(zone);
            var gc = StartLoopAddressCoarseOffset(zone);
            return g + 0x8000 * gc;
        }

        public static short EndLoopAddressOffset(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.EndLoopAddressOffset);
            return g?.Int16Amount ?? 0;
        }

        public static short EndLoopAddressCoarseOffset(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.EndLoopAddressCoarseOffset);
            return g?.Int16Amount ?? 0;
        }

        public static int FullEndLoopAddressOffset(this Generator[] zone) {
            var g = EndLoopAddressOffset(zone);
            var gc = EndLoopAddressCoarseOffset(zone);
            return g + 0x8000 * gc;
        }

        public static ushort KeyRange(this Zone zone) {
            return zone.Generators.KeyRange();
        }

        public static ushort KeyRange(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.KeyRange);
            return g?.UInt16Amount ?? 0;
        }

        public static ushort VelocityRange(this Zone zone) {
            return zone.Generators.VelocityRange();
        }

        public static ushort VelocityRange(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.VelocityRange);
            return g?.UInt16Amount ?? 0;
        }

        public static byte Velocity(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.Velocity);
            return g?.LowByteAmount ?? 0;
        }

        public static byte OverridingRootKey(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.OverridingRootKey);
            return g?.LowByteAmount ?? 0;
        }

        public static double Pan(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.Pan);
            return g?.Int16Amount / 500d ?? 0;
        }

        public static double Attenuation(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.InitialAttenuation);
            return g?.Int16Amount / 10d ?? 0;
        }

        public static sbyte Correction(this Generator[] zone) {
            var sh = zone.SampleHeader();
            return sh?.PitchCorrection ?? 0;
        }

        public static short CoarseTune(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.CoarseTune);
            return g?.Int16Amount ?? 0;
        }

        public static short FineTune(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.FineTune);
            return g?.Int16Amount ?? 0;
        }

        public static int TotalCorrection(this Generator[] zone) {
            return Correction(zone) + CoarseTune(zone) * 100 + FineTune(zone);
        }

        public static byte Key(this Generator[] zone) {
            var sh = zone.SampleHeader();
            if (sh == null)
                return 0;

            byte over = zone.OverridingRootKey();
            return over != 0 ? over : sh.OriginalPitch;
        }

        public static short ScaleTuning(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.ScaleTuning);
            return g?.Int16Amount ?? 100;
        }

        public static int SampleModes(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.SampleModes);
            return g?.UInt16Amount ?? 0;
        }

        public static SampleHeader SampleHeader(this Zone zone) {
            return zone.Generators.SampleHeader();
        }

        public static SampleHeader SampleHeader(this Generator[] zone) {
            var g = SelectByGenerator(zone, GeneratorEnum.SampleID);
            return g?.SampleHeader;
        }

        public static Generator SelectByGenerator(this Generator[] zone, GeneratorEnum type) {
            foreach (var g in zone)
                if (g.GeneratorType == type)
                    return g;
            return null;
        }
    }
}

using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class SampleSoundGenerator {
        public WaveStream Wave { get; set; }
        public int KeyCorrection { get; set; }
        public float VolumeCorrection { get; set; }
        public double FadeStart { get; set; }
        public double FadeLength { get; set; }

        public SampleSoundGenerator(WaveStream wave) {
            Wave = wave;
            KeyCorrection = -1;
            VolumeCorrection = -1;
            FadeStart = -1;
            FadeLength = -1;
        }

        public SampleSoundGenerator(WaveStream wave, double fadeStart, double fadeLength) {
            Wave = wave;
            KeyCorrection = -1;
            VolumeCorrection = -1;
            FadeStart = fadeStart;
            FadeLength = fadeLength;
        }

        public ISampleProvider GetSampleProvider() {
            Wave.Position = 0;
            ISampleProvider output = new Pcm16BitToSampleProvider(Wave);

            if (FadeStart != -1 && FadeLength != -1) {
                output = new DelayFadeOutSampleProvider(output);
                (output as DelayFadeOutSampleProvider).BeginFadeOut(FadeStart * 1000, FadeLength * 1000);
            }
            if (KeyCorrection != -1) {
                output = SampleImporter.PitchShift(output, KeyCorrection);
            }
            if (VolumeCorrection != -1) {
                output = SampleImporter.VolumeChange(output, VolumeCorrection);
            }
            return output;
        }
    }
}

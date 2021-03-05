using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio {
    public static class Helpers {
        public static ISampleProvider SetChannels(ISampleProvider sampleProvider, int channels) {
            return channels == 1 ? ToMono(sampleProvider) : ToStereo(sampleProvider);
        }

        private static ISampleProvider ToStereo(ISampleProvider sampleProvider) {
            return sampleProvider.WaveFormat.Channels == 1 ? new MonoToStereoSampleProvider(sampleProvider) : sampleProvider;
        }

        private static ISampleProvider ToMono(ISampleProvider sampleProvider) {
            return sampleProvider.WaveFormat.Channels == 2 ? new StereoToMonoSampleProvider(sampleProvider) : sampleProvider;
        }
    }
}
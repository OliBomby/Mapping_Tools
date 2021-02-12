using Mapping_Tools_Core.Audio.Exporting;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public class RawAudioSampleGenerator : IAudioSampleGenerator {
        public WaveStream WaveStream { get; }

        public RawAudioSampleGenerator(WaveStream waveStream) {
            WaveStream = waveStream;
        }

        public bool Equals(ISampleGenerator other) {
            return other is RawAudioSampleGenerator o && WaveStream.Equals(o.WaveStream);
        }

        public object Clone() {
            return new RawAudioSampleGenerator(WaveStream); 
        }

        public bool IsValid() {
            return WaveStream != null;
        }

        public ISampleProvider GetSampleProvider() {
            WaveStream.Position = 0;
            return new WaveToSampleProvider(WaveStream);
        }

        public string GetName() {
            return WaveStream.ToString();
        }

        public void ToExporter(ISampleExporter exporter) {
            if (exporter is IAudioSampleExporter audioSampleExporter) {
                audioSampleExporter.AddAudio(GetSampleProvider());
            }
        }

        public void PreLoadSample() { }
    }
}
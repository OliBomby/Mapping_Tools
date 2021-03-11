using System;
using Mapping_Tools_Core.Audio.Exporting;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public class RawAudioSampleGenerator : IAudioSampleGenerator {
        protected WaveStream WaveStream { get; }

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

        public double GetAmplitudeFactor() {
            return 1;
        }

        public string GetName() {
            return WaveStream.ToString();
        }

        public virtual void ToExporter(ISampleExporter exporter) {
            if (exporter is IAudioSampleExporter audioSampleExporter) {
                audioSampleExporter.AddAudio(GetSampleProvider());

                audioSampleExporter.BlankSample = audioSampleExporter.BlankSample &&
                                                  WaveStream.TotalTime.Equals(TimeSpan.Zero);
                // If the encoding of the source is float then clipping is possible
                audioSampleExporter.ClippingPossible = audioSampleExporter.ClippingPossible ||
                                                       WaveStream.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat;
            }
        }

        public void PreloadSample() { }
    }
}
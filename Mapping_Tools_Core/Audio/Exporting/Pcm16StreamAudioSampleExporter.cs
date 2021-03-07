using System.IO;
using Mapping_Tools_Core.Audio.Effects;
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.Exporting {
    public class Pcm16StreamAudioSampleExporter : StreamAudioSampleExporter {
        private readonly bool useLimiter;

        public Pcm16StreamAudioSampleExporter(bool useLimiter = true) {
            this.useLimiter = useLimiter;
        }

        public Pcm16StreamAudioSampleExporter(Stream outStream, bool useLimiter = true) : base(outStream) {
            this.useLimiter = useLimiter;
        }

        protected override bool ExportSampleProvider(ISampleProvider sampleProvider, int numTracks) {
            // I really want to check the entire audio sample here to see if it has any clipping,
            // but that is too memory intensive.
            if (useLimiter && (ClippingPossible || numTracks > 1)) {
                sampleProvider = new SoftLimiter(sampleProvider);
            }

            var sourceProvider = sampleProvider.ToWaveProvider16();

            return Helpers.CreateWaveFile(OutStream, sourceProvider);
        }

        public override string GetDesiredExtension() {
            return @".wav";
        }

        public override WaveFormatEncoding? GetDesiredWaveEncoding() {
            return WaveFormatEncoding.Pcm;
        }
    }
}
using System.IO;
using Mapping_Tools_Core.Audio.Effects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio.Exporting {
    public class VorbisStreamAudioSampleExporter : StreamAudioSampleExporter {
        private readonly float quality;
        private readonly bool useLimiter;

        public VorbisStreamAudioSampleExporter(float quality = 0.5f, bool useLimiter = true) {
            this.quality = quality;
            this.useLimiter = useLimiter;
        }

        public VorbisStreamAudioSampleExporter(Stream outStream, float quality = 0.5f, bool useLimiter = true) : base(outStream) {
            this.quality = quality;
            this.useLimiter = useLimiter;
        }

        protected override bool ExportSampleProvider(ISampleProvider sampleProvider, int numTracks) {
            // I really want to check the entire audio sample here to see if it has any clipping,
            // but that is too memory intensive.
            if (useLimiter && (ClippingPossible || numTracks > 1)) {
                sampleProvider = new SoftLimiter(sampleProvider);
            }

            var resampled = new WdlResamplingSampleProvider(sampleProvider,
                VorbisFileWriter.GetSupportedSampleRate(sampleProvider.WaveFormat.SampleRate));

            return VorbisFileWriter.CreateVorbisFile(OutStream, resampled.ToWaveProvider(), quality);
        }

        public override string GetDesiredExtension() {
            return @".ogg";
        }

        public override WaveFormatEncoding? GetDesiredWaveEncoding() {
            return null;
        }
    }
}
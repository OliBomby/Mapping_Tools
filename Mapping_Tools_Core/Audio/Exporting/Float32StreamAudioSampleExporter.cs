using System.IO;
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.Exporting {
    public class Float32StreamAudioSampleExporter : StreamAudioSampleExporter {
        public Float32StreamAudioSampleExporter() { }

        public Float32StreamAudioSampleExporter(Stream outStream) : base(outStream) { }

        protected override bool ExportSampleProvider(ISampleProvider sampleProvider, int numTracks) {
            var sourceProvider = sampleProvider.ToWaveProvider();

            return Helpers.CreateWaveFile(OutStream, sourceProvider);
        }

        public override string GetDesiredExtension() {
            return @".wav";
        }

        public override WaveFormatEncoding? GetDesiredWaveEncoding() {
            return WaveFormatEncoding.IeeeFloat;
        }
    }
}
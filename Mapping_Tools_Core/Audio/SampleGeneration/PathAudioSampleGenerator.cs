using System;
using Mapping_Tools_Core.Audio.SampleImporters;
using NAudio.Wave;
using System.IO;
using System.Linq;
using Mapping_Tools_Core.Audio.Exporting;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    /// <summary>
    /// Generates audio from a file in the file system.
    /// Works with <see cref="IPathAudioSampleExporter"/>.
    /// </summary>
    public class PathAudioSampleGenerator : IAudioSampleGenerator, IFromPathGenerator {
        private static readonly string[] ValidExtensions = {".wav", ".mp3", ".aiff", ".ogg"};

        protected string Extension() => System.IO.Path.GetExtension(Path);

        protected WaveStream cachedWaveStream;
        protected bool preloaded;

        public string Path { get; }

        public PathAudioSampleGenerator(string path) {
            Path = path;
        }

        public bool Equals(ISampleGenerator other) {
            return other is PathAudioSampleGenerator o && Path.Equals(o.Path);
        }

        public object Clone() {
            return new PathAudioSampleGenerator(Path); 
        }

        public bool IsValid() {
            if (preloaded) {
                return cachedWaveStream != null;
            }

            return File.Exists(Path) && ValidExtensions.Contains(Extension());
        }

        private static ISampleProvider GetSampleProvider(WaveStream wave) {
            wave.Position = 0;
            return new WaveToSampleProvider(wave);
        }

        public ISampleProvider GetSampleProvider() {
            return GetSampleProvider(GetWaveStream());
        }

        public double GetAmplitudeFactor() {
            return 1;
        }

        private WaveStream GetWaveStream() {
            if (preloaded) {
                return cachedWaveStream;
            }

            return Extension() == ".ogg" ? new VorbisFileImporter(Path).Import() : new AudioFileImporter(Path).Import();
        }

        public string GetName() {
            return System.IO.Path.GetFileNameWithoutExtension(Path);
        }

        public virtual void ToExporter(ISampleExporter exporter) {
            var wave = GetWaveStream();

            if (exporter is IAudioSampleExporter audioSampleExporter) {
                audioSampleExporter.AddAudio(GetSampleProvider(wave));

                audioSampleExporter.BlankSample = audioSampleExporter.BlankSample && 
                                                  wave.TotalTime.Equals(TimeSpan.Zero);
                // If the encoding of the source is float then clipping is possible
                audioSampleExporter.ClippingPossible = audioSampleExporter.ClippingPossible ||
                                                       wave.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat;
            }

            if (exporter is IPathAudioSampleExporter pathAudioSampleExporter) {
                pathAudioSampleExporter.CopyPath = Path;
            }
        }

        public void PreloadSample() {
            if (!preloaded) {
                try {
                    cachedWaveStream = GetWaveStream();
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }

                preloaded = true;
            }
        }
    }
}
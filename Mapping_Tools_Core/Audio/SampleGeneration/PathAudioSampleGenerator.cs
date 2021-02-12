using System;
using Mapping_Tools_Core.Audio.SampleImporters;
using NAudio.Wave;
using System.IO;
using System.Linq;
using Mapping_Tools_Core.Audio.Exporting;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public class PathAudioSampleGenerator : IAudioSampleGenerator {
        private static readonly string[] ValidExtensions = {".wav", ".mp3", ".aiff", ".ogg"};

        private string Extension() => System.IO.Path.GetExtension(Path);

        private WaveStream cachedWaveStream;
        private bool preloaded;

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

        public ISampleProvider GetSampleProvider() {
            var wave = GetWaveStream();
            wave.Position = 0;
            return new WaveToSampleProvider(wave);
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

        public void ToExporter(ISampleExporter exporter) {
            if (exporter is IAudioSampleExporter audioSampleExporter) {
                audioSampleExporter.AddAudio(GetSampleProvider());
            }

            if (exporter is IPathAudioSampleExporter pathAudioSampleExporter) {
                pathAudioSampleExporter.CopyPath = Path;
            }
        }

        public void PreLoadSample() {
            if (!preloaded) {
                preloaded = true;

                try {
                    cachedWaveStream = GetWaveStream();
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
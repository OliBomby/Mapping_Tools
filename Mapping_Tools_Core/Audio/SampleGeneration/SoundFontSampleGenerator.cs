using Mapping_Tools_Core.Audio.Exporting;
using Mapping_Tools_Core.Audio.Midi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public class SoundFontSampleGenerator : ISoundFontSampleGenerator {
        private string Extension() => System.IO.Path.GetExtension(Path);

        private WaveStream cachedWaveStream;
        private bool preloaded;

        public string Path { get; }
        public IMidiNote Note { get; }

        public SoundFontSampleGenerator(string path, IMidiNote note) {
            Path = path;
            Note = note;
        }

        public bool Equals(ISampleGenerator other) {
            return other is SoundFontSampleGenerator o &&
                   Path.Equals(o.Path) &&
                   Note.Equals(o.Note);
        }

        public object Clone() {
            return new SoundFontSampleGenerator(Path, Note);
        }

        public bool IsValid() {
            if (preloaded) {
                return cachedWaveStream != null;
            }

            return File.Exists(Path) && Extension() == ".sf2";
        }

        public ISampleProvider GetSampleProvider() {
            return GetSampleProvider(GetWaveStream());
        }

        public string GetName() {
            var filename = System.IO.Path.GetFileNameWithoutExtension(Path);
            return $"{filename}-{Note}";
        }

        public void ToExporter(ISampleExporter exporter) {
            if (exporter is IAudioSampleExporter audioSampleExporter) {
                audioSampleExporter.AddAudio(GetSampleProvider());
            }
        }

        public void PreLoadSample() {
            if (!preloaded) {
                preloaded = true;

                try {
                    cachedWaveStream = GetWaveStream();
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }

        private static ISampleProvider GetSampleProvider(WaveStream wave) {
            wave.Position = 0;
            return new WaveToSampleProvider(wave);
        }

        private WaveStream GetWaveStream() {
            if (preloaded) {
                return cachedWaveStream;
            }

            throw new NotImplementedException();
        }
    }
}
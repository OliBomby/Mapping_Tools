using Mapping_Tools_Core.Audio.Exporting;
using Mapping_Tools_Core.Audio.Midi;
using NAudio.Wave;
using System;
using System.IO;
using Mapping_Tools_Core.Audio.SampleImporters;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public class SoundFontSampleGenerator : ISoundFontSampleGenerator, IFromPathGenerator {
        protected string Extension() => System.IO.Path.GetExtension(Path);

        protected IAudioSampleGenerator cachedGenerator;
        protected bool preloaded;

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
                return cachedGenerator != null;
            }

            return File.Exists(Path) && Extension() == ".sf2";
        }

        public ISampleProvider GetSampleProvider() {
            return GetSampleGenerator().GetSampleProvider();
        }

        public double GetAmplitudeFactor() {
            return GetSampleGenerator().GetAmplitudeFactor();
        }

        public string GetName() {
            var filename = System.IO.Path.GetFileNameWithoutExtension(Path);
            return $"{filename}-{Note}";
        }

        public virtual void ToExporter(ISampleExporter exporter) {
            if (exporter is IAudioSampleExporter audioSampleExporter) {
                GetSampleGenerator()?.ToExporter(audioSampleExporter);
            }

            if (exporter is IMidiSampleExporter midiSampleExporter) {
                midiSampleExporter.AddMidiNote(Note);
            }
        }

        public void PreloadSample() {
            if (!preloaded) {
                try {
                    cachedGenerator = GetSampleGenerator();
                } catch (Exception e) {
                    Console.WriteLine(e);
                }

                preloaded = true;
            }
        }

        private IAudioSampleGenerator GetSampleGenerator() {
            if (preloaded) {
                return cachedGenerator;
            }

            return new SoundFontSampleImporter(Path).Import(Note);
        }
    }
}
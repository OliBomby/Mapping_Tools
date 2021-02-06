using System.Collections.Generic;
using System.IO;
using Mapping_Tools_Core.Audio.SampleImporters;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public class VorbisFileImportArgs : IVorbisFileImportArgs {
        private string Extension => System.IO.Path.GetExtension(Path);

        public VorbisFileImportArgs(string path) {
            Path = path;
        }

        public bool Equals(ISampleGenerator other) {
            return other is IVorbisFileImportArgs o && Path.Equals(o.Path);
        }

        public object Clone() {
            return new VorbisFileImportArgs(Path);
        }

        public bool IsValid() {
            return File.Exists(Path) && Extension == ".ogg";
        }

        public bool IsValid(Dictionary<ISampleGenerator, ISampleSoundGenerator> loadedSamples) {
            return loadedSamples.ContainsKey(this) && loadedSamples[this] != null;
        }

        public ISampleSoundGenerator Import() {
            return new VorbisFileImporter().Import(this);
        }

        public string GetName() {
            return System.IO.Path.GetFileNameWithoutExtension(Path);
        }

        public string Path { get; }
    }
}
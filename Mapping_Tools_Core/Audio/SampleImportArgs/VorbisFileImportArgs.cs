using System.Collections.Generic;
using System.IO;
using Mapping_Tools_Core.Audio.SampleImporters;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;

namespace Mapping_Tools_Core.Audio.SampleImportArgs {
    public class VorbisFileImportArgs : IVorbisFileImportArgs {
        private string Extension => System.IO.Path.GetExtension(Path);

        public VorbisFileImportArgs(string path) {
            Path = path;
        }

        public bool Equals(ISampleImportArgs other) {
            return other is IVorbisFileImportArgs o && Path.Equals(o.Path);
        }

        public object Clone() {
            return new VorbisFileImportArgs(Path);
        }

        public bool IsValid() {
            return File.Exists(Path) && Extension == ".ogg";
        }

        public bool IsValid(Dictionary<ISampleImportArgs, ISampleSoundGenerator> loadedSamples) {
            return loadedSamples.ContainsKey(this) && loadedSamples[this] != null;
        }

        public ISampleSoundGenerator Import() {
            return new VorbisFileImporter().Import(this);
        }

        public string Path { get; }
    }
}
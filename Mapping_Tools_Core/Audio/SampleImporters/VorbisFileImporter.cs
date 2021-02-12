using NAudio.Vorbis;
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleImporters {
    public class VorbisFileImporter {
        private readonly string path;

        public VorbisFileImporter(string path) {
            this.path = path;
        }

        public WaveStream Import() {
            return new VorbisWaveReader(path);
        }
    }
}
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleImporters {
    public class AudioFileImporter {
        private readonly string path;

        public AudioFileImporter(string path) {
            this.path = path;
        }

        public WaveStream Import() {
            return new AudioFileReader(path);
        }
    }
}
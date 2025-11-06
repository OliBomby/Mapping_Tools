using NAudio.Wave;

namespace Mapping_Tools.Core.Audio.SampleImporters;

public class AudioFileImporter {
    private readonly string path;

    public AudioFileImporter(string path) {
        this.path = path;
    }

    public WaveStream Import() {
        return Helpers.OpenSample(path);
    }
}
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleImporters {
    public class AudioFileImporter : ISampleImporter<IAudioFileImportArgs> {
        public ISampleSoundGenerator Import(IAudioFileImportArgs args) {
            return new WaveStreamSampleSoundGenerator(new AudioFileReader(args.Path));
        }
    }
}
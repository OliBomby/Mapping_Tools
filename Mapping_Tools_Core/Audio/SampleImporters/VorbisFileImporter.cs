using Mapping_Tools_Core.Audio.SampleImportArgs;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using NAudio.Vorbis;

namespace Mapping_Tools_Core.Audio.SampleImporters {
    public class VorbisFileImporter : ISampleImporter<IVorbisFileImportArgs> {
        public ISampleSoundGenerator Import(IVorbisFileImportArgs args) {
            return new WaveStreamSampleSoundGenerator(new VorbisWaveReader(args.Path));
        }
    }
}
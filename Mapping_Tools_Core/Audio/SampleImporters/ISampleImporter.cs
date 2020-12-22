using Mapping_Tools_Core.Audio.SampleImportArgs;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;

namespace Mapping_Tools_Core.Audio.SampleImporters {
    public interface ISampleImporter<in T> where T : ISampleImportArgs {
        ISampleSoundGenerator Import(T args);
    }
}
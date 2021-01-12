using Mapping_Tools_Core.Audio.SampleImportArgs;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;

namespace Mapping_Tools_Core.Audio.SampleImporters {
    public interface ISampleImporter<in T> where T : ISampleImportArgs {
        /// <summary>
        /// Imports hitsound layers using the arguments.
        /// </summary>
        /// <returns>The imported hitsound layers</returns>
        ISampleSoundGenerator Import(T args);
    }
}
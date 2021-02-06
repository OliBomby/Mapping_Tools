using System.Collections.Generic;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Exporting {
    public interface IExtendedSampleExporter<T> : ISampleExporter<T> where T : ISampleGeneratingArgs {
        void ExportLoadedSamples(Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples,
            string exportFolder, Dictionary<ISampleGeneratingArgs, string> names = null);
    }
}
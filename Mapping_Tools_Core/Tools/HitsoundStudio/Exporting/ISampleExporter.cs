using System.Collections.Generic;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Exporting {
    public interface ISampleExporter<T> where T : ISampleGeneratingArgs {
        void ExportSample(T sampleGeneratingArgs, string name);
        void ExportMixedSample(ICollection<T> sampleGeneratingArgs, string name);
    }
}
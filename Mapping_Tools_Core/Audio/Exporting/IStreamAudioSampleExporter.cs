using System.IO;

namespace Mapping_Tools_Core.Audio.Exporting {
    public interface IStreamAudioSampleExporter : IAudioSampleExporter {
        Stream ExportStream { get; set; }
    }
}
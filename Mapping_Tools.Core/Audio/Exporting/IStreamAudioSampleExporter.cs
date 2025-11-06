using System.IO;

namespace Mapping_Tools.Core.Audio.Exporting;

public interface IStreamAudioSampleExporter : IAudioSampleExporter {
    Stream OutStream { get; set; }
}
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.Exporting {
    public interface IAudioSampleExporter : ISampleExporter {
        /// <summary>
        /// Adds a sample audio stream to the exporter to be exported.
        /// </summary>
        /// <param name="sample">The audio to export.</param>
        void AddAudio(ISampleProvider sample);
    }
}
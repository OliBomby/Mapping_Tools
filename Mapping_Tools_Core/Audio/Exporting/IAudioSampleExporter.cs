using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.Exporting {
    public interface IAudioSampleExporter : ISampleExporter {
        /// <summary>
        /// Adds a sample audio stream to the exporter to be exported.
        /// </summary>
        /// <param name="sample">The audio to export.</param>
        void AddAudio(ISampleProvider sample);

        /// <summary>
        /// Returns the last added audio and removes it from the exporter.
        /// Returns null if there was never anything added.
        /// </summary>
        /// <returns></returns>
        ISampleProvider PopAudio();

        /// <summary>
        /// Returns the file extension that fits with the type of the exporter.
        /// Returns null if not applicable.
        /// </summary>
        /// <returns></returns>
        string GetDesiredExtension();

        /// <summary>
        /// Returns the wave encoding that fits with the export of the exporter.
        /// Returns null if not applicable.
        /// </summary>
        /// <returns></returns>
        WaveFormatEncoding? GetDesiredWaveEncoding();
    }
}
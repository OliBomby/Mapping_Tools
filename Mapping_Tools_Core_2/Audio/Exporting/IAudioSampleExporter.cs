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
        /// Returns the wave encoding that fits with the export of the exporter.
        /// Returns null if not applicable.
        /// </summary>
        /// <returns></returns>
        WaveFormatEncoding? GetDesiredWaveEncoding();

        /// <summary>
        /// Whether the sample is blank.
        /// Default to true.
        /// </summary>
        bool BlankSample { get; set; }

        /// <summary>
        /// Whether clipping is possible in the sample.
        /// Default to false.
        /// </summary>
        bool ClippingPossible { get; set; }
    }
}
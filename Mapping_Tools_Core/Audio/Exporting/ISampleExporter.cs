namespace Mapping_Tools_Core.Audio.Exporting {
    public interface ISampleExporter {
        /// <summary>
        /// Flushes data and exports the sample.
        /// Also resets.
        /// </summary>
        /// <returns>Whether the expor</returns>
        bool Flush();

        /// <summary>
        /// Resets the exporter, returning it to a state where no samples are added.
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns the file extension that fits with the type of the exporter.
        /// Returns null if not applicable.
        /// </summary>
        /// <returns></returns>
        string GetDesiredExtension();
    }
}
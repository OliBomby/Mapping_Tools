namespace Mapping_Tools_Core.Tools.HitsoundStudio.Exporting {
    public interface ISampleExporter {
        /// <summary>
        /// Flushes data and exports the sample.
        /// </summary>
        /// <returns>Whether the expor</returns>
        bool Flush();
    }
}
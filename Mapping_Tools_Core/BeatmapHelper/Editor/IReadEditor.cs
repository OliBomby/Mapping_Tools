namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    /// <summary>
    /// Read interface a file./>
    /// </summary>
    public interface IReadEditor<out T> : IEditor {
        /// <summary>
        /// Reads and parses <see cref="IEditor.Path"/>.
        /// </summary>
        /// <returns>The parsed object</returns>
        T ReadFile();
    }
}
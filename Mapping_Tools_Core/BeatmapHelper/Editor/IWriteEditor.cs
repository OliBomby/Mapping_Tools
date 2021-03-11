namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    /// <summary>
    /// Write interface a file./>
    /// </summary>
    public interface IWriteEditor<in T> : IEditor {
        /// <summary>
        /// Writes the given instance to <see cref="IEditor.Path"/>.
        /// </summary>
        /// <param name="instance">The object to write</param>
        void WriteFile(T instance);
    }
}
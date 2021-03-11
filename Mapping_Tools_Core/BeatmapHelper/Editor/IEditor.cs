namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    public interface IEditor {
        /// <summary>
        /// The path to the file to edit.
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// Grabs the parent folder as absolute.
        /// </summary>
        /// <returns>The parent folder of <see cref="Path"/></returns>
        string GetParentFolder();
    }
}
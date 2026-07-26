using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
/// Provides typed editing and filename-aware saving for an osu! beatmap.
/// </summary>
public class BeatmapEditor2 : Editor2 {
    /// <summary>
    /// Gets the parsed beatmap document.
    /// </summary>
    public Beatmap Beatmap => (Beatmap)TextFile;

    /// <summary>
    /// Creates a beatmap editor from serialized lines.
    /// </summary>
    /// <param name="lines">The beatmap lines to parse.</param>
    /// <param name="fileStore">The persistence implementation used when saving.</param>
    public BeatmapEditor2(List<string> lines, ITextFileStore fileStore) : base(fileStore) {
        TextFile = new Beatmap(lines);
    }

    /// <summary>
    /// Loads a beatmap from disk through the supplied persistence boundary.
    /// </summary>
    /// <param name="path">The beatmap file to load.</param>
    /// <param name="fileStore">The persistence implementation used to load and save.</param>
    public BeatmapEditor2(string path, ITextFileStore fileStore) : base(fileStore) {
        Path = path;
        TextFile = new Beatmap(ReadFile(path));
    }

    /// <summary>
    /// Saves the beatmap and updates its filename from the beatmap metadata.
    /// </summary>
    /// <remarks>This method also updates <see cref="Editor2.Path"/>.</remarks>
    public void SaveFileWithNameUpdate() {
        var parentFolder = GetParentFolder();
        FileStore.Delete(Path);
        Path = FileStore.CombinePath(parentFolder, Beatmap.GetFileName());
        SaveFile();
    }
}

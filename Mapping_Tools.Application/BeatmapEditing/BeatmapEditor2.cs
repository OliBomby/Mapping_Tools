using Mapping_Tools.ApplicationServices.Abstractions;

namespace Mapping_Tools.Classes.BeatmapHelper;

public class BeatmapEditor2 : Editor2 {
    public Beatmap Beatmap => (Beatmap)TextFile;

    public BeatmapEditor2(List<string> lines, ITextFileStore fileStore) : base(fileStore) {
        TextFile = new Beatmap(lines);
    }

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

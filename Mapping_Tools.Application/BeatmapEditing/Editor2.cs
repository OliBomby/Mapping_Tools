using Mapping_Tools.ApplicationServices.Abstractions;

namespace Mapping_Tools.Classes.BeatmapHelper;

/// <summary>
/// Edits an osu! text file using caller-provided persistence.
/// </summary>
public class Editor2 {
    protected ITextFileStore FileStore { get; }

    public string Path { get; set; } = string.Empty;

    public ITextFile TextFile { get; set; } = null!;

    public Editor2(ITextFileStore fileStore) {
        FileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    }

    public Editor2(List<string> lines, ITextFileStore fileStore) : this(fileStore) {
        TextFile = new Beatmap(lines);
    }

    public Editor2(string path, ITextFileStore fileStore) : this(fileStore) {
        Path = path;
        var lines = ReadFile(path);
        TextFile = path.EndsWith(".osb", StringComparison.OrdinalIgnoreCase)
            ? new StoryBoard(lines)
            : new Beatmap(lines);
    }

    public List<string> ReadFile(string path) => new(FileStore.ReadAllLines(path));

    public virtual void SaveFile(string path) {
        var lines = TextFile.GetLines();
        BeforeSave(lines);
        FileStore.WriteAllLines(path, lines);
    }

    public virtual void SaveFile(List<string> lines) {
        BeforeSave(lines);
        FileStore.WriteAllLines(Path, lines);
    }

    public virtual void SaveFile() {
        var lines = TextFile.GetLines();
        BeforeSave(lines);
        FileStore.WriteAllLines(Path, lines);
    }

    public static void SaveFile(ITextFileStore fileStore, string path, List<string> lines) {
        ArgumentNullException.ThrowIfNull(fileStore);
        fileStore.WriteAllLines(path, lines);
    }

    public string GetParentFolder() => FileStore.GetParentFolder(Path);

    public static string GetParentFolder(ITextFileStore fileStore, string path) {
        ArgumentNullException.ThrowIfNull(fileStore);
        return fileStore.GetParentFolder(path);
    }

    protected virtual void BeforeSave(List<string> lines) { }
}

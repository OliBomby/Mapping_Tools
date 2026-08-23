using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
/// Edits an osu! text file using caller-provided persistence.
/// </summary>
public class Editor2 {
    /// <summary>
    /// Gets the persistence boundary used for all file and path operations.
    /// </summary>
    protected ITextFileStore FileStore { get; }

    /// <summary>
    /// Identifies the source file and the destination used by parameterless
    /// saves; assigning it retargets future writes without reloading the document.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Owns the mutable parsed document that will be serialized on save. Its
    /// runtime type is <see cref="Beatmap"/> or <see cref="StoryBoard"/>
    /// according to the editor that loaded it.
    /// </summary>
    public ITextFile TextFile { get; set; } = null!;

    /// <summary>
    /// Creates an editor without loading a document.
    /// </summary>
    /// <param name="fileStore">The persistence implementation used by the editor.</param>
    public Editor2(ITextFileStore fileStore) {
        FileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    }

    /// <summary>
    /// Creates an editor for an in-memory beatmap.
    /// </summary>
    /// <param name="lines">The serialized beatmap lines to parse.</param>
    /// <param name="fileStore">The persistence implementation used when saving.</param>
    public Editor2(List<string> lines, ITextFileStore fileStore) : this(fileStore) {
        TextFile = new Beatmap(lines);
    }

    /// <summary>
    /// Loads and parses an osu! beatmap or storyboard from a path.
    /// </summary>
    /// <param name="path">The source file; <c>.osb</c> selects storyboard parsing.</param>
    /// <param name="fileStore">The persistence implementation used to load and save.</param>
    public Editor2(string path, ITextFileStore fileStore) : this(fileStore) {
        Path = path;
        var lines = ReadFile(path);
        TextFile = path.EndsWith(".osb", StringComparison.OrdinalIgnoreCase)
            ? new StoryBoard(lines)
            : new Beatmap(lines);
    }

    /// <summary>
    /// Reads a text file through the configured persistence boundary.
    /// </summary>
    /// <param name="path">The source file.</param>
    /// <returns>A mutable list containing the file's lines.</returns>
    public List<string> ReadFile(string path) {
        // Get contents of the file
        return new(FileStore.ReadAllLines(path));
    }

    /// <summary>
    /// Serializes the current document and writes it to a new path.
    /// </summary>
    /// <param name="path">The destination path. This does not change <see cref="Path"/>.</param>
    public virtual void SaveFile(string path) {
        var lines = TextFile.GetLines();
        BeforeSave(lines);
        FileStore.WriteAllLines(path, lines);
    }

    /// <summary>
    /// Writes caller-supplied serialized lines to <see cref="Path"/>.
    /// </summary>
    /// <param name="lines">The complete serialized document.</param>
    public virtual void SaveFile(List<string> lines) {
        BeforeSave(lines);
        FileStore.WriteAllLines(Path, lines);
    }

    /// <summary>
    /// Serializes the current document and writes it to <see cref="Path"/>.
    /// </summary>
    public virtual void SaveFile() {
        var lines = TextFile.GetLines();
        BeforeSave(lines);
        FileStore.WriteAllLines(Path, lines);
    }

    /// <summary>
    /// Writes serialized lines with an explicitly supplied persistence implementation.
    /// </summary>
    /// <param name="fileStore">The persistence implementation to use.</param>
    /// <param name="path">The destination file.</param>
    /// <param name="lines">The complete serialized document.</param>
    public static void SaveFile(ITextFileStore fileStore, string path, List<string> lines) {
        ArgumentNullException.ThrowIfNull(fileStore);
        fileStore.WriteAllLines(path, lines);
    }

    /// <summary>
    /// Gets the directory containing <see cref="Path"/>.
    /// </summary>
    /// <returns>The document's parent directory.</returns>
    public string GetParentFolder() => FileStore.GetParentFolder(Path);

    /// <summary>
    /// Resolves a path's parent using an explicitly supplied persistence implementation.
    /// </summary>
    /// <param name="fileStore">The persistence implementation to use.</param>
    /// <param name="path">The path whose parent is required.</param>
    /// <returns>The containing directory.</returns>
    public static string GetParentFolder(ITextFileStore fileStore, string path) {
        ArgumentNullException.ThrowIfNull(fileStore);
        return fileStore.GetParentFolder(path);
    }

    /// <summary>
    /// Allows specialized editors to coordinate external state immediately
    /// before serialized lines are persisted.
    /// </summary>
    /// <param name="lines">The exact lines that will be written.</param>
    protected virtual void BeforeSave(List<string> lines) { }
}

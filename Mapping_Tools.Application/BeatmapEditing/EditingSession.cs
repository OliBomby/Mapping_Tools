using Mapping_Tools.Application.Abstractions;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Provides persistence operations for a typed, mutable text document.
/// </summary>
public abstract class EditingSession
{
    /// <summary>
    ///     Creates a session backed by the supplied text-file store.
    /// </summary>
    /// <param name="fileStore">The persistence implementation used by the session.</param>
    protected EditingSession(ITextFileStore fileStore)
    {
        FileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    }

    /// <summary>
    ///     Gets the persistence boundary used for file and path operations.
    /// </summary>
    protected ITextFileStore FileStore { get; }

    /// <summary>
    ///     Identifies the source file and destination used by parameterless saves.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    ///     Reads a complete text file through the configured persistence boundary.
    /// </summary>
    /// <param name="path">The source file.</param>
    /// <returns>The complete file contents.</returns>
    public string ReadFile(string path)
    {
        return FileStore.ReadAllText(path);
    }

    /// <summary>
    ///     Encodes and saves the current document to a new path without changing <see cref="Path" />.
    /// </summary>
    /// <param name="path">The destination path.</param>
    public void SaveFile(string path)
    {
        SaveFile(FileStore, path, EncodeDocument());
    }

    /// <summary>
    ///     Encodes and saves the current document to <see cref="Path" />.
    /// </summary>
    public void SaveFile()
    {
        SaveFile(FileStore, Path, EncodeDocument());
    }

    /// <summary>
    ///     Writes already serialized text through an explicitly supplied store.
    /// </summary>
    /// <param name="fileStore">The persistence implementation to use.</param>
    /// <param name="path">The destination file.</param>
    /// <param name="text">The complete serialized document.</param>
    public static void SaveFile(ITextFileStore fileStore, string path, string text)
    {
        ArgumentNullException.ThrowIfNull(fileStore);
        ArgumentNullException.ThrowIfNull(text);
        fileStore.WriteAllText(path, text);
    }

    /// <summary>
    ///     Gets the directory containing <see cref="Path" />.
    /// </summary>
    /// <returns>The document's parent directory.</returns>
    public string GetParentFolder()
    {
        return FileStore.GetParentFolder(Path);
    }

    /// <summary>
    ///     Resolves a path's parent using an explicitly supplied persistence implementation.
    /// </summary>
    /// <param name="fileStore">The persistence implementation to use.</param>
    /// <param name="path">The path whose parent is required.</param>
    /// <returns>The containing directory.</returns>
    public static string GetParentFolder(ITextFileStore fileStore, string path)
    {
        ArgumentNullException.ThrowIfNull(fileStore);
        return fileStore.GetParentFolder(path);
    }

    /// <summary>
    ///     Encodes the session's current document for persistence.
    /// </summary>
    /// <returns>The complete serialized document.</returns>
    protected abstract string EncodeDocument();

    internal string GetSerializedText()
    {
        return EncodeDocument();
    }
}

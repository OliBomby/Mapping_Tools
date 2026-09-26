namespace Mapping_Tools.Application.Abstractions;

/// <summary>
///     Abstracts the text-file and path operations used by beatmap editors so
///     parsing and editing can be tested without touching the physical filesystem.
/// </summary>
public interface ITextFileStore
{
    /// <summary>
    ///     Reads the complete contents of a text file.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <returns>The complete file contents.</returns>
    string ReadAllText(string path);

    /// <summary>
    ///     Replaces a text file with the supplied text.
    /// </summary>
    /// <param name="path">The destination file.</param>
    /// <param name="text">The complete contents to write.</param>
    void WriteAllText(string path, string text);

    /// <summary>
    ///     Deletes a file.
    /// </summary>
    /// <param name="path">The file to delete.</param>
    void Delete(string path);

    /// <summary>
    ///     Resolves the directory containing a file or directory path.
    /// </summary>
    /// <param name="path">The path whose parent is required.</param>
    /// <returns>The parent directory path.</returns>
    string GetParentFolder(string path);

    /// <summary>
    ///     Combines a directory with a child path using platform path semantics.
    /// </summary>
    /// <param name="parent">The parent directory.</param>
    /// <param name="child">The child file or directory name.</param>
    /// <returns>The combined path.</returns>
    string CombinePath(string parent, string child);
}

using Mapping_Tools.ApplicationServices.Abstractions;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>
/// Implements beatmap-editor persistence with <see cref="File"/>,
/// <see cref="Directory"/>, and <see cref="Path"/>.
/// </summary>
public sealed class FileSystemFileStore : ITextFileStore {
    /// <summary>
    /// <inheritdoc/>
    public IReadOnlyList<string> ReadAllLines(string path) => File.ReadAllLines(path);

    /// <summary>
    /// <inheritdoc/>
    public void WriteAllLines(string path, IEnumerable<string> lines) => File.WriteAllLines(path, lines);

    /// <summary>
    /// <inheritdoc/>
    public void Delete(string path) => File.Delete(path);

    /// <summary>
    /// <inheritdoc/>
    public string GetParentFolder(string path) {
        return Directory.GetParent(path)?.FullName
               ?? throw new DirectoryNotFoundException($"Path '{path}' does not have a parent folder.");
    }

    /// <summary>
    /// <inheritdoc/>
    public string CombinePath(string parent, string child) => Path.Combine(parent, child);
}

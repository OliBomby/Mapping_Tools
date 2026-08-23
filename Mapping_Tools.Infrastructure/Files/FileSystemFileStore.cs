using System.Text;
using Mapping_Tools.Application.Abstractions;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>
///     Implements beatmap-editor persistence with <see cref="File" />,
///     <see cref="Directory" />, and <see cref="Path" />.
/// </summary>
public sealed class FileSystemFileStore : ITextFileStore
{
    private static readonly Encoding utf8WithoutBom = new UTF8Encoding(false);

    /// <summary>
    ///     <inheritdoc />
    public IReadOnlyList<string> ReadAllLines(string path)
    {
        return File.ReadAllLines(path);
    }

    /// <summary>
    ///     <inheritdoc />
    public void WriteAllLines(string path, IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(lines);

        if (!path.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllLines(path, lines);
            return;
        }

        using StreamWriter writer = new(path, false, utf8WithoutBom)
        {
            NewLine = "\r\n",
        };
        foreach (string line in lines) writer.WriteLine(line);
    }

    /// <summary>
    ///     <inheritdoc />
    public void Delete(string path)
    {
        File.Delete(path);
    }

    /// <summary>
    ///     <inheritdoc />
    public string GetParentFolder(string path)
    {
        return Directory.GetParent(path)?.FullName
               ?? throw new DirectoryNotFoundException($"Path '{path}' does not have a parent folder.");
    }

    /// <summary>
    ///     <inheritdoc />
    public string CombinePath(string parent, string child)
    {
        return Path.Combine(parent, child);
    }
}

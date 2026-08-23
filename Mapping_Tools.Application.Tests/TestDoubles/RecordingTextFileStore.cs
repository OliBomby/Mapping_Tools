using Mapping_Tools.Application.Abstractions;

namespace Mapping_Tools.Application.Tests.TestDoubles;

internal sealed class RecordingTextFileStore : ITextFileStore
{
    public RecordingTextFileStore()
    {
    }

    public RecordingTextFileStore(string path, IEnumerable<string> lines)
    {
        Files[path] = lines.ToList();
    }

    public Dictionary<string, List<string>> Files { get; } =
        new(StringComparer.Ordinal);

    public List<string> ReadPaths { get; } = [];

    public List<(string Path, IReadOnlyList<string> Lines)> WriteRequests { get; } = [];

    public List<string> DeletedPaths { get; } = [];

    public int ReadCount => ReadPaths.Count;

    public int WriteCount => WriteRequests.Count;

    public Func<string, string>? ParentFolderResolver { get; init; }

    public Func<string, string, string>? CombinePathResolver { get; init; }

    public IReadOnlyList<string> ReadAllLines(string path)
    {
        ReadPaths.Add(path);
        return Files[path].ToList();
    }

    public void WriteAllLines(string path, IEnumerable<string> lines)
    {
        IReadOnlyList<string> copiedLines = lines.ToArray();
        WriteRequests.Add((path, copiedLines));
        Files[path] = copiedLines.ToList();
    }

    public void Delete(string path)
    {
        DeletedPaths.Add(path);
        Files.Remove(path);
    }

    public string GetParentFolder(string path)
    {
        return ParentFolderResolver?.Invoke(path)
            ?? Path.GetDirectoryName(path)
            ?? string.Empty;
    }

    public string CombinePath(string parent, string child)
    {
        return CombinePathResolver?.Invoke(parent, child)
            ?? Path.Combine(parent, child);
    }
}

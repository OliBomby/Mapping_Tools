using Mapping_Tools.Application.Abstractions;

namespace Mapping_Tools.Application.Tests.TestDoubles;

internal sealed class NoOpTextFileStore : ITextFileStore
{
    public string? ReadResult { get; init; }

    public Func<string, string>? ParentFolderResolver { get; init; }

    public Func<string, string, string>? CombinePathResolver { get; init; }

    public string ReadAllText(string path)
    {
        return ReadResult
               ?? throw new NotSupportedException("Reading was not configured for this test store.");
    }

    public void WriteAllText(string path, string text)
    {
    }

    public void Delete(string path)
    {
    }

    public string GetParentFolder(string path)
    {
        return ParentFolderResolver?.Invoke(path) ?? string.Empty;
    }

    public string CombinePath(string parent, string child)
    {
        return CombinePathResolver?.Invoke(parent, child) ?? child;
    }
}

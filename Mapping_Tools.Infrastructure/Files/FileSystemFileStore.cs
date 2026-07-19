using Mapping_Tools.ApplicationServices.Abstractions;

namespace Mapping_Tools.Infrastructure.Files;

public sealed class FileSystemFileStore : ITextFileStore {
    public IReadOnlyList<string> ReadAllLines(string path) => File.ReadAllLines(path);

    public void WriteAllLines(string path, IEnumerable<string> lines) => File.WriteAllLines(path, lines);

    public void Delete(string path) => File.Delete(path);

    public string GetParentFolder(string path) {
        return Directory.GetParent(path)?.FullName
               ?? throw new DirectoryNotFoundException($"Path '{path}' does not have a parent folder.");
    }

    public string CombinePath(string parent, string child) => Path.Combine(parent, child);
}

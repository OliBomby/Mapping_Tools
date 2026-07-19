namespace Mapping_Tools.ApplicationServices.Abstractions;

public interface ITextFileStore {
    IReadOnlyList<string> ReadAllLines(string path);
    void WriteAllLines(string path, IEnumerable<string> lines);
    void Delete(string path);
    string GetParentFolder(string path);
    string CombinePath(string parent, string child);
}

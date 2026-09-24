using System.Text;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Infrastructure.Files;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc2;

/// <summary>Routes osu! Songs documents through MTIPC2 and other files to Mapping Tools storage.</summary>
public sealed class Mtipc2TextFileStore : ITextFileStore, IAutomaticBeatmapFilenameStore
{
    private readonly ApplicationSettings settings;
    private readonly Mtipc2Client client;
    private readonly PhysicalBeatmapsetFileSystem local;

    /// <summary>Creates the MTIPC2 store and its local application-file fallback.</summary>
    public Mtipc2TextFileStore(ApplicationSettings settings, Mtipc2Client client,
        PhysicalBeatmapsetFileSystem local)
    {
        this.settings = settings;
        this.client = client;
        this.local = local;
    }

    internal bool TryFileId(string path, out string fileId)
    {
        fileId = string.Empty;
        if (string.IsNullOrWhiteSpace(settings.SongsPath)) return false;
        string root = Path.GetFullPath(settings.SongsPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(path);
        if (!full.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            return false;
        fileId = Path.GetRelativePath(root, full).Replace('\\', '/');
        return fileId.Split('/').Length >= 2;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ReadAllLines(string path)
    {
        if (!TryFileId(path, out string id)) return local.ReadAllLines(path);
        using var reader = new StringReader(Encoding.UTF8.GetString(client.ReadFile(id)));
        var lines = new List<string>();
        while (reader.ReadLine() is { } line) lines.Add(line);
        return lines;
    }

    /// <inheritdoc />
    public void WriteAllLines(string path, IEnumerable<string> lines)
    {
        if (!TryFileId(path, out string id))
        {
            local.WriteAllLines(path, lines);
            return;
        }
        string mapsetId = id.Split('/')[0];
        bool exists = client.ListFiles(mapsetId).Any(file => string.Equals(file.Id, id, StringComparison.OrdinalIgnoreCase));
        byte[] bytes = Encoding.UTF8.GetBytes(string.Join("\r\n", lines) + "\r\n");
        client.Commit(new Mtipc2Change(exists ? "replace" : "create", id, bytes));
    }

    /// <inheritdoc />
    public string WriteBeatmap(string path, IEnumerable<string> lines)
    {
        if (!TryFileId(path, out string id))
        {
            local.WriteAllLines(path, lines);
            return path;
        }
        byte[] bytes = Encoding.UTF8.GetBytes(string.Join("\r\n", lines) + "\r\n");
        bool exists = client.ListFiles(id.Split('/')[0]).Any(file =>
            string.Equals(file.Id, id, StringComparison.OrdinalIgnoreCase));
        string newId = client.Commit(new Mtipc2Change(exists ? "replace" : "create", id, bytes)).Single().FileId;
        return Path.GetFullPath(Path.Combine(settings.SongsPath, newId.Replace('/', Path.DirectorySeparatorChar)));
    }

    /// <inheritdoc />
    public void Delete(string path)
    {
        if (TryFileId(path, out string id)) client.Commit(new Mtipc2Change("delete", id));
        else local.Delete(path);
    }

    /// <inheritdoc />
    public string GetParentFolder(string path) => Path.GetDirectoryName(Path.GetFullPath(path))
        ?? throw new DirectoryNotFoundException(path);

    /// <inheritdoc />
    public string CombinePath(string parent, string child) => Path.Combine(parent, child);
}

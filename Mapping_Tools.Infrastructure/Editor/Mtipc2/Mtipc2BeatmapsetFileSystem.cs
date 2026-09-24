using System.IO.Enumeration;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Infrastructure.Files;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc2;

/// <summary>Routes osu! Songs file operations through MTIPC2 while keeping Mapping Tools exports local.</summary>
public sealed class Mtipc2BeatmapsetFileSystem : IBeatmapsetFileSystem
{
    private readonly ApplicationSettings settings;
    private readonly Mtipc2Client client;
    private readonly Mtipc2TextFileStore text;
    private readonly PhysicalBeatmapsetFileSystem local;

    /// <summary>Creates the proof-of-concept mapset file system.</summary>
    public Mtipc2BeatmapsetFileSystem(ApplicationSettings settings, Mtipc2Client client,
        Mtipc2TextFileStore text, PhysicalBeatmapsetFileSystem local)
    {
        this.settings = settings;
        this.client = client;
        this.text = text;
        this.local = local;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ReadAllLines(string path) => text.ReadAllLines(path);

    /// <inheritdoc />
    public void WriteAllLines(string path, IEnumerable<string> lines) => text.WriteAllLines(path, lines);

    /// <inheritdoc />
    public void Delete(string path) => text.Delete(path);

    /// <inheritdoc />
    public string GetParentFolder(string path) => text.GetParentFolder(path);

    /// <inheritdoc />
    public string CombinePath(string parent, string child) => text.CombinePath(parent, child);

    /// <inheritdoc />
    public bool FileExists(string path)
    {
        if (!text.TryFileId(path, out string id)) return local.FileExists(path);
        return client.ListFiles(id.Split('/')[0]).Any(file =>
            string.Equals(file.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        if (IsSongsRoot(path))
            return client.ListMapsets() is not null;
        if (!TryMapsetId(path, out string mapsetId)) return local.DirectoryExists(path);
        if (!client.ListMapsets().Any(mapset => string.Equals(mapset.Id, mapsetId, StringComparison.OrdinalIgnoreCase)))
            return false;
        string relative = Path.GetRelativePath(settings.SongsPath, path).Replace('\\', '/');
        if (!relative.Contains('/')) return true;
        return client.ListFiles(mapsetId).Any(file => file.Id.StartsWith(relative.TrimEnd('/') + '/', StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public string? GetParentDirectory(string filePath) => Path.GetDirectoryName(Path.GetFullPath(filePath));

    /// <inheritdoc />
    public IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern,
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (IsSongsRoot(directory))
        {
            if (searchOption == SearchOption.TopDirectoryOnly) return [];
            return client.ListMapsets()
                .SelectMany(mapset => client.ListFiles(mapset.Id))
                .Select(file => Path.GetFullPath(Path.Combine(settings.SongsPath,
                    file.Id.Replace('/', Path.DirectorySeparatorChar))))
                .Where(path => FileSystemName.MatchesSimpleExpression(searchPattern, Path.GetFileName(path), ignoreCase: true))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        if (!TryMapsetId(directory, out string mapsetId))
            return local.EnumerateFiles(directory, searchPattern, searchOption);
        string root = Path.GetFullPath(settings.SongsPath);
        string fullDirectory = Path.GetFullPath(directory);
        return client.ListFiles(mapsetId)
            .Select(file => Path.GetFullPath(Path.Combine(root, file.Id.Replace('/', Path.DirectorySeparatorChar))))
            .Where(path => path.StartsWith(fullDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            .Where(path => searchOption == SearchOption.AllDirectories
                           || string.Equals(Path.GetDirectoryName(path), fullDirectory, StringComparison.OrdinalIgnoreCase))
            .Where(path => FileSystemName.MatchesSimpleExpression(searchPattern, Path.GetFileName(path), ignoreCase: true))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc />
    public void EnsureDirectoryExists(string path)
    {
        if (!IsSongsRoot(path) && !TryMapsetId(path, out _)) local.EnsureDirectoryExists(path);
        else if (!DirectoryExists(path)) throw new DirectoryNotFoundException(path);
    }

    /// <inheritdoc />
    public byte[] ReadAllBytes(string path) => text.TryFileId(path, out string id)
        ? client.ReadFile(id) : local.ReadAllBytes(path);

    /// <inheritdoc />
    public void WriteAllBytes(string path, ReadOnlySpan<byte> bytes, bool overwrite = false)
    {
        if (!text.TryFileId(path, out string id))
        {
            local.WriteAllBytes(path, bytes, overwrite);
            return;
        }
        bool exists = FileExists(path);
        if (exists && !overwrite) throw new IOException($"'{path}' already exists.");
        client.Commit(new Mtipc2Change(exists ? "replace" : "create", id, bytes.ToArray()));
    }

    /// <inheritdoc />
    public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false) =>
        WriteAllBytes(destinationPath, ReadAllBytes(sourcePath), overwrite);

    /// <inheritdoc />
    public void MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        if (text.TryFileId(sourcePath, out string sourceId) &&
            text.TryFileId(destinationPath, out string destinationId))
        {
            if (FileExists(destinationPath) && !overwrite) throw new IOException($"'{destinationPath}' already exists.");
            client.Commit(new Mtipc2Change(FileExists(destinationPath) ? "replace" : "create",
                    destinationId, client.ReadFile(sourceId)),
                new Mtipc2Change("delete", sourceId));
            return;
        }
        CopyFile(sourcePath, destinationPath, overwrite);
        Delete(sourcePath);
    }

    /// <inheritdoc />
    public IBeatmapsetFileTransaction BeginTransaction(string targetDirectory)
    {
        if (TryMapsetId(targetDirectory, out _))
            throw new NotSupportedException("MTIPC2 proof of concept does not stage transactions inside osu! Songs.");
        return local.BeginTransaction(targetDirectory);
    }

    private bool TryMapsetId(string path, out string mapsetId)
    {
        mapsetId = string.Empty;
        if (string.IsNullOrWhiteSpace(settings.SongsPath)) return false;
        string root = Path.GetFullPath(settings.SongsPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(path);
        if (!full.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            return false;
        mapsetId = Path.GetRelativePath(root, full).Replace('\\', '/').Split('/')[0];
        return mapsetId.Length > 0;
    }

    private bool IsSongsRoot(string path) =>
        !string.IsNullOrWhiteSpace(settings.SongsPath) &&
        string.Equals(Path.GetFullPath(path), Path.GetFullPath(settings.SongsPath),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}

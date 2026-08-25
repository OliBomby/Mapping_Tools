using System.IO.Compression;
using System.Text;
using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Application.Tools.PatternGallery.Models;

namespace Mapping_Tools.Infrastructure.PatternGallery;

/// <summary>Creates and reads Pattern Gallery ZIP files with traversal checks.</summary>
public sealed class PatternGalleryArchiveService : IPatternGalleryArchiveService
{
    private static readonly Encoding utf8WithoutBom = new UTF8Encoding(false);

    /// <inheritdoc />
    public Task ExportAsync(
        string archivePath,
        string collectionFolderName,
        string projectFileName,
        string projectJson,
        IReadOnlyList<PatternGalleryArchiveFile> patternFiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ValidateSegment(collectionFolderName, nameof(collectionFolderName));
        ValidateSegment(projectFileName, nameof(projectFileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(projectJson);
        ArgumentNullException.ThrowIfNull(patternFiles);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(archivePath))!);
        using FileStream stream = new(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using ZipArchive archive = new(stream, ZipArchiveMode.Create);
        var projectEntry = archive.CreateEntry($"{collectionFolderName}/{projectFileName}");
        using (var projectStream = projectEntry.Open())
        using (StreamWriter writer = new(projectStream, utf8WithoutBom, leaveOpen: false))
        {
            writer.Write(projectJson);
        }

        foreach (var patternFile in patternFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateSegment(patternFile.FileName, nameof(patternFile.FileName));
            var entry = archive.CreateEntry(
                $"{collectionFolderName}/Pattern Files/{patternFile.FileName}");
            using var destination = entry.Open();
            destination.Write(patternFile.Content, 0, patternFile.Content.Length);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<PatternGalleryArchive> ReadAsync(
        string archivePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = ZipFile.OpenRead(archivePath);
        string? collectionFolder = null;
        string? projectFileName = null;
        string? projectJson = null;
        List<PatternGalleryArchiveFile> patterns = [];
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string[] parts = ValidateEntry(entry.FullName);
            collectionFolder ??= parts[0];
            if (!string.Equals(collectionFolder, parts[0], StringComparison.Ordinal))
                throw new InvalidDataException("A Pattern Gallery archive must contain one root collection folder.");

            if (parts.Length == 2 && parts[1].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                projectFileName ??= parts[1];
                using var stream = entry.Open();
                using StreamReader reader = new(stream, utf8WithoutBom);
                projectJson ??= reader.ReadToEnd();
            }
            else if (parts.Length >= 3
                     && string.Equals(parts[1], "Pattern Files", StringComparison.OrdinalIgnoreCase)
                     && parts[^1].EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = entry.Open();
                using MemoryStream memory = new();
                stream.CopyTo(memory);
                patterns.Add(new PatternGalleryArchiveFile(parts[^1], memory.ToArray()));
            }
        }

        if (collectionFolder is null || projectFileName is null || projectJson is null) throw new InvalidDataException("The archive must contain one project JSON file.");

        return Task.FromResult(new PatternGalleryArchive(
            collectionFolder,
            projectFileName,
            projectJson,
            patterns));
    }

    /// <inheritdoc />
    public Task ExtractAsync(
        string archivePath,
        string basePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = ZipFile.OpenRead(archivePath);
        string destinationRoot = Path.GetFullPath(basePath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string[] parts = ValidateEntry(entry.FullName);
            string destination = parts.Aggregate(destinationRoot, Path.Combine);
            string? directory = Path.GetDirectoryName(destination);
            if (directory is not null) Directory.CreateDirectory(directory);

            if (string.IsNullOrEmpty(entry.Name)) continue;

            using var source = entry.Open();
            using FileStream target = new(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            source.CopyTo(target);
        }

        return Task.CompletedTask;
    }

    private static string[] ValidateEntry(string entryName)
    {
        string normalized = entryName.Replace('\\', '/');
        string[] parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (string.IsNullOrWhiteSpace(entryName)
            || Path.IsPathRooted(entryName)
            || normalized.StartsWith("/", StringComparison.Ordinal)
            || parts.Length == 0
            || parts.Any(part => part is "." or ".." || Path.IsPathRooted(part) || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new InvalidDataException("The archive contains an unsafe path.");

        return parts;
    }

    private static void ValidateSegment(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Contains('/') || value.Contains('\\') || value is "." or ".." || Path.IsPathRooted(value) || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("The value must be one relative file or folder name.", parameterName);
    }
}

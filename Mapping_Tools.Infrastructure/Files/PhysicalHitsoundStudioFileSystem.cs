using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>Provides the ordinary local filesystem operations needed by export.</summary>
public sealed class PhysicalHitsoundStudioFileSystem : IHitsoundStudioFileSystem
{
    /// <inheritdoc />
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    /// <inheritdoc />
    public void DeleteFiles(string path)
    {
        foreach (string file in Directory.EnumerateFiles(path)) File.Delete(file);
    }

    /// <inheritdoc />
    public void CopyFile(string sourcePath, string destinationPath)
    {
        string? directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.Copy(sourcePath, destinationPath, true);
    }
}

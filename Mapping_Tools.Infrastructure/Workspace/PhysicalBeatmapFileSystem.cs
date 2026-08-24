using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Infrastructure.Workspace;

/// <summary>
///     Checks selected beatmap paths against the local filesystem and derives
///     their containing directories for native picker start locations.
/// </summary>
public sealed class PhysicalBeatmapFileSystem : IBeatmapFileSystem
{
    /// <inheritdoc />
    public bool FileExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.Exists(path);
    }

    /// <inheritdoc />
    public string? GetParentDirectory(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Path.GetDirectoryName(Path.GetFullPath(filePath));
    }
}

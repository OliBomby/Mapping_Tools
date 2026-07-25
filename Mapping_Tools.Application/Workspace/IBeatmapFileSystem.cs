namespace Mapping_Tools.ApplicationServices.Workspace;

/// <summary>
/// Supplies the path checks needed by current-map state without tying the
/// Application layer to the physical filesystem.
/// </summary>
public interface IBeatmapFileSystem
{
    /// <summary>
    /// Determines whether a selected beatmap path currently resolves to a file.
    /// </summary>
    /// <param name="path">The local path recorded by the workspace.</param>
    /// <returns><see langword="true"/> only when the file currently exists.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Resolves the folder a beatmap picker should initially display.
    /// </summary>
    /// <param name="filePath">A local beatmap file path.</param>
    /// <returns>The containing directory, or <see langword="null"/> when it has none.</returns>
    string? GetParentDirectory(string filePath);
}

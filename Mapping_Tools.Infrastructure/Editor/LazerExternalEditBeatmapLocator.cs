namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Finds osu!lazer's temporary external-edit mount and a beatmap within it.
///     Lazer mounts beatmap sets in SHA-256-named directories beneath the system temp folder.
/// </summary>
public sealed class LazerExternalEditBeatmapLocator
{
    private readonly string tempDirectory;
    private string? preferredPath;

    /// <summary>Uses the current process's temporary directory.</summary>
    public LazerExternalEditBeatmapLocator() : this(Path.GetTempPath())
    {
    }

    /// <summary>Uses a caller-supplied temporary directory.</summary>
    /// <param name="tempDirectory">The root containing lazer's temporary external-edit mounts.</param>
    public LazerExternalEditBeatmapLocator(string tempDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tempDirectory);
        this.tempDirectory = Path.GetFullPath(tempDirectory);
    }

    /// <summary>
    ///     Finds a beatmap in the newest mounted set, preferring a difficulty explicitly
    ///     selected in Mapping Tools while that set remains mounted.
    /// </summary>
    /// <returns>The mounted beatmap path, or <see langword="null" /> when no set is mounted.</returns>
    public string? FindMountedBeatmap()
    {
        try
        {
            if (!Directory.Exists(tempDirectory)) return null;

            var mountedSets = Directory.EnumerateDirectories(tempDirectory)
                .Where(IsMountDirectory)
                .Select(directory => new
                {
                    Directory = directory,
                    Beatmaps = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                        .Where(path => string.Equals(
                            Path.GetExtension(path), ".osu", StringComparison.OrdinalIgnoreCase))
                        .ToArray(),
                })
                .Where(set => set.Beatmaps.Length > 0)
                .OrderByDescending(set => Directory.GetCreationTimeUtc(set.Directory))
                .ToArray();

            if (mountedSets.Length == 0) return null;

            string[] beatmaps = mountedSets[0].Beatmaps;
            string? preferred = preferredPath;
            if (preferred is not null && beatmaps.Contains(preferred, StringComparer.OrdinalIgnoreCase))
                return preferred;

            return beatmaps
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .First();
        }
        catch (IOException)
        {
            // Lazer may remove or replace a mount while it is being enumerated.
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Prefers a user-selected difficulty inside an active external-edit mount.</summary>
    /// <param name="path">The selected beatmap path.</param>
    public void Prefer(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (IsMountDirectory(Path.GetDirectoryName(path)) && File.Exists(path))
            preferredPath = path;
    }

    private static bool IsMountDirectory(string? path)
    {
        if (path is null) return false;

        string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        return name.Length == 64 && name.All(Uri.IsHexDigit);
    }
}

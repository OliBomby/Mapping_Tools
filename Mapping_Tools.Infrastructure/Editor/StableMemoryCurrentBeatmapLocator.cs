using System.Diagnostics;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>Locates the loaded osu!stable beatmap on Windows or under Wine on Linux.</summary>
public sealed class StableMemoryCurrentBeatmapLocator : ICurrentBeatmapLocator
{
    private static readonly TimeSpan attachTimeout = TimeSpan.FromSeconds(2);
    private readonly ApplicationSettings settings;
    private readonly Func<bool> isSupported;
    private readonly Func<bool> canRead;
    private readonly Func<CurrentBeatmap?> readBeatmap;

    /// <summary>Creates a memory reader using the configured native Songs directory.</summary>
    /// <param name="settings">Settings containing the osu! Songs directory.</param>
    public StableMemoryCurrentBeatmapLocator(ApplicationSettings settings)
        : this(settings,
            () => OperatingSystem.IsWindows() || OperatingSystem.IsLinux(),
            () => CurrentBeatmapMemoryReader.CanRead,
            CurrentBeatmapMemoryReader.TryRead)
    {
    }

    internal StableMemoryCurrentBeatmapLocator(ApplicationSettings settings,
        Func<bool> isSupported, Func<bool> canRead, Func<CurrentBeatmap?> readBeatmap)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.isSupported = isSupported ?? throw new ArgumentNullException(nameof(isSupported));
        this.canRead = canRead ?? throw new ArgumentNullException(nameof(canRead));
        this.readBeatmap = readBeatmap ?? throw new ArgumentNullException(nameof(readBeatmap));
    }

    /// <inheritdoc />
    public async Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!isSupported())
            throw new InvalidOperationException(
                "Current osu! beatmap lookup through process memory is unavailable on this platform.");

        if (string.IsNullOrWhiteSpace(settings.SongsPath))
            throw new InvalidOperationException(
                "Set the osu! Songs folder in Mapping Tools preferences before using memory reading.");

        // The shared reader discovers and reconnects to the process in the background.
        var stopwatch = Stopwatch.StartNew();
        while (!canRead())
        {
            if (stopwatch.Elapsed >= attachTimeout)
                throw new InvalidOperationException(
                    "Unable to attach to osu!. Open a beatmap in osu! and try again.");

            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        }

        var beatmap = await Task.Run(readBeatmap, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (beatmap is null || string.IsNullOrWhiteSpace(beatmap.FolderName)
                            || string.IsNullOrWhiteSpace(beatmap.OsuFileName))
            throw new InvalidOperationException(
                "Open a beatmap in osu! before using the current editor state.");

        string path = Path.GetFullPath(Path.Combine(settings.SongsPath,
            beatmap.FolderName.Replace('\\', Path.DirectorySeparatorChar),
            beatmap.OsuFileName.Replace('\\', Path.DirectorySeparatorChar)));
        if (!File.Exists(path))
            throw new InvalidOperationException(
                "The current beatmap file does not exist. Check the osu! Songs folder in Mapping Tools preferences.");

        return path;
    }
}

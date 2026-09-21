using System.Diagnostics;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Locates osu!'s current beatmap through the stable client's process memory.
/// </summary>
public sealed class WindowsMemoryCurrentBeatmapLocator : ICurrentBeatmapLocator
{
    private readonly ApplicationSettings settings;
    private readonly Func<Process?> findProcess;
    private readonly Func<Process, string?> readCurrentBeatmap;

    /// <summary>Initializes the memory-backed current-beatmap locator.</summary>
    /// <param name="settings">Settings containing osu!'s Songs directory.</param>
    public WindowsMemoryCurrentBeatmapLocator(ApplicationSettings settings)
        : this(
            settings,
            OperatingSystem.IsWindows,
            OsuProcessDiscovery.FindStableProcess,
            process => CurrentBeatmapMemoryReader.TryRead(process, settings.SongsPath))
    {
    }

    internal WindowsMemoryCurrentBeatmapLocator(
        ApplicationSettings settings,
        Func<bool> isWindows,
        Func<Process?> findProcess,
        Func<Process, string?> readCurrentBeatmap)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
        this.findProcess = findProcess ?? throw new ArgumentNullException(nameof(findProcess));
        this.readCurrentBeatmap = readCurrentBeatmap ?? throw new ArgumentNullException(nameof(readCurrentBeatmap));
    }

    private readonly Func<bool> isWindows;

    /// <inheritdoc />
    public async Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
    {
        if (!isWindows())
            throw new InvalidOperationException(
                "Current osu! beatmap lookup through process memory is unavailable on this platform.");

        cancellationToken.ThrowIfCancellationRequested();
        using var process = findProcess();
        if (process is null)
            throw new InvalidOperationException(
                "Open a beatmap in osu! before using the current editor state.");

        string? path = await Task.Run(
                // ReSharper disable once AccessToDisposedClosure
                () => readCurrentBeatmap(process),
                cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException(
                "Open a beatmap in osu! before using the current editor state.");

        return Path.GetFullPath(path);
    }
}

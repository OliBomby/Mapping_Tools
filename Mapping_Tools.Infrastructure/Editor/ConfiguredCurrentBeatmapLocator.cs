using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Selects the configured current-beatmap locator without combining or falling
///     back between backends.
/// </summary>
public sealed class ConfiguredCurrentBeatmapLocator : ICurrentBeatmapLocator
{
    private readonly ICurrentBeatmapLocator memoryReader;
    private readonly ICurrentBeatmapLocator mtipcReader;
    private readonly ICurrentBeatmapLocator gosumemoryReader;
    private readonly LazerExternalEditBeatmapLocator? lazerExternalEdit;
    private readonly ApplicationSettings settings;

    /// <summary>Initializes a current-beatmap backend selector with all supported readers.</summary>
    /// <param name="settings">The settings that choose the active reader.</param>
    /// <param name="memoryReader">Reads the current beatmap from editor process memory.</param>
    /// <param name="mtipcReader">Reads the current beatmap through MTIPC.</param>
    /// <param name="gosumemoryReader">Reads the current beatmap from the local gosumemory API.</param>
    /// <param name="lazerExternalEdit">Finds an active osu!lazer external-edit mount.</param>
    public ConfiguredCurrentBeatmapLocator(
        ApplicationSettings settings,
        ICurrentBeatmapLocator memoryReader,
        ICurrentBeatmapLocator mtipcReader,
        ICurrentBeatmapLocator gosumemoryReader,
        LazerExternalEditBeatmapLocator? lazerExternalEdit = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
        this.mtipcReader = mtipcReader ?? throw new ArgumentNullException(nameof(mtipcReader));
        this.gosumemoryReader = gosumemoryReader ?? throw new ArgumentNullException(nameof(gosumemoryReader));
        this.lazerExternalEdit = lazerExternalEdit;
    }

    /// <inheritdoc />
    public Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (settings.AutoDetectLazerExternalEdit)
        {
            string? mountedPath = lazerExternalEdit?.FindMountedBeatmap();
            if (mountedPath is not null) return Task.FromResult(mountedPath);
        }

        return settings.CurrentBeatmapFetching switch
        {
            CurrentBeatmapFetchingMode.Disabled => throw new InvalidOperationException(
                "Current beatmap fetching is disabled in Mapping Tools settings."),
            CurrentBeatmapFetchingMode.Mtipc => mtipcReader.FindCurrentBeatmapAsync(cancellationToken),
            CurrentBeatmapFetchingMode.Gosumemory => gosumemoryReader.FindCurrentBeatmapAsync(cancellationToken),
            _ => memoryReader.FindCurrentBeatmapAsync(cancellationToken),
        };
    }
}

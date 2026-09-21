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
    private readonly ApplicationSettings settings;

    /// <summary>Initializes a current-beatmap backend selector.</summary>
    public ConfiguredCurrentBeatmapLocator(
        ApplicationSettings settings,
        ICurrentBeatmapLocator memoryReader,
        ICurrentBeatmapLocator mtipcReader)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.memoryReader = memoryReader ?? throw new ArgumentNullException(nameof(memoryReader));
        this.mtipcReader = mtipcReader ?? throw new ArgumentNullException(nameof(mtipcReader));
    }

    /// <inheritdoc />
    public Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return settings.CurrentBeatmapFetching switch
        {
            CurrentBeatmapFetchingMode.Disabled => throw new InvalidOperationException(
                "Current beatmap fetching is disabled in Mapping Tools settings."),
            CurrentBeatmapFetchingMode.Mtipc => mtipcReader.FindCurrentBeatmapAsync(cancellationToken),
            _ => memoryReader.FindCurrentBeatmapAsync(cancellationToken),
        };
    }
}

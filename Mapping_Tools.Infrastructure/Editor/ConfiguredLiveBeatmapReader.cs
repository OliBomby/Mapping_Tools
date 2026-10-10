using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Selects exactly one configured live beatmap-state reader.
/// </summary>
public sealed class ConfiguredLiveBeatmapReader : ILiveBeatmapReader
{
    private readonly ILiveBeatmapReader editorReader;
    private readonly ILiveBeatmapReader mtipcReader;
    private readonly ApplicationSettings settings;

    /// <summary>Initializes a live-state backend selector.</summary>
    public ConfiguredLiveBeatmapReader(
        ApplicationSettings settings,
        ILiveBeatmapReader editorReader,
        ILiveBeatmapReader mtipcReader)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.editorReader = editorReader ?? throw new ArgumentNullException(nameof(editorReader));
        this.mtipcReader = mtipcReader ?? throw new ArgumentNullException(nameof(mtipcReader));
    }

    /// <inheritdoc />
    public Task<LiveBeatmapSnapshot?> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return settings.BeatmapLiveStateReading switch
        {
            BeatmapLiveStateReadingMode.Disabled => Task.FromResult<LiveBeatmapSnapshot?>(null),
            BeatmapLiveStateReadingMode.Mtipc => mtipcReader.ReadAsync(cancellationToken),
            _ => editorReader.ReadAsync(cancellationToken),
        };
    }
}

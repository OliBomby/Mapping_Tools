using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Supplies no live editor state on platforms without the Windows osu!
///     memory and Editor Reader integration.
/// </summary>
public sealed class UnsupportedPlatformLiveBeatmapReader : ILiveBeatmapReader
{
    /// <inheritdoc />
    public Task<LiveBeatmapSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<LiveBeatmapSnapshot?>(null);
    }
}

using Mapping_Tools.Application.BeatmapEditing.Contracts;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Ignores BetterSave watcher configuration on platforms without the
///     Windows osu! foreground-window integration.
/// </summary>
public sealed class UnsupportedPlatformBetterSaveOverrideService : IBetterSaveOverrideService
{
    /// <inheritdoc />
    public void Configure(string songsPath, bool enabled)
    {
    }

    /// <inheritdoc />
    public void Stop()
    {
    }
}

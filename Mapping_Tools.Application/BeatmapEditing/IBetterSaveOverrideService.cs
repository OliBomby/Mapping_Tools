using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Controls the platform watcher that replaces focused osu! saves with BetterSave output.
/// </summary>
public interface IBetterSaveOverrideService
{
    /// <summary>
    ///     Reconfigures recursive beatmap observation after a path or enabled preference changes.
    /// </summary>
    /// <param name="songsPath">The osu! beatmap-library root to observe.</param>
    /// <param name="enabled">Whether matching saves should invoke BetterSave.</param>
    void Configure(string songsPath, bool enabled);

    /// <summary>Stops observation and releases platform watcher resources.</summary>
    void Stop();
}


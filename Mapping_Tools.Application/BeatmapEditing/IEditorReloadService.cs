using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Requests that osu! discard its cached view of the current file and load the
///     freshly written version from disk.
/// </summary>
public interface IEditorReloadService
{
    /// <summary>
    ///     Sends the reload gesture to an active osu! editor, or does nothing when
    ///     osu! is closed or has no usable window.
    /// </summary>
    /// <param name="cancellationToken">Cancels before or between input operations.</param>
    /// <returns>A task that completes after the reload gesture is delivered or skipped.</returns>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}


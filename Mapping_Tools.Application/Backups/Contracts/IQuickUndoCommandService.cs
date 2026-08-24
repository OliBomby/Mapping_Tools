using Mapping_Tools.Application.Backups.Models;

namespace Mapping_Tools.Application.Backups.Contracts;

/// <summary>
///     Resolves osu!'s current beatmap and applies the newest retained backup using
///     the same operation from both in-app actions and global shortcuts.
/// </summary>
public interface IQuickUndoCommandService
{
    /// <summary>
    ///     Attempts one restore and publishes a frontend-neutral outcome message.
    /// </summary>
    /// <param name="cancellationToken">Cancels current-map lookup, backup replacement, or editor reload.</param>
    /// <returns>A typed outcome; ordinary lookup and restore failures are captured.</returns>
    Task<QuickUndoCommandResult> ExecuteAsync(
        CancellationToken cancellationToken = default);
}


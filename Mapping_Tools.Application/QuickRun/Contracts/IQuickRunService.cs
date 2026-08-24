using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.QuickRun.Contracts;

/// <summary>
///     Resolves and invokes the current or configured Smart QuickRun command from
///     live osu! selection state.
/// </summary>
public interface IQuickRunService
{
    /// <summary>
    ///     Applies the current settings, resolves one command, and awaits its quick callback.
    /// </summary>
    /// <param name="cancellationToken">Cancels live selection discovery or the feature callback.</param>
    /// <returns>A typed dispatch outcome; ordinary live-read and command failures are captured.</returns>
    Task<QuickRunResult> RunAsync(CancellationToken cancellationToken = default);
}


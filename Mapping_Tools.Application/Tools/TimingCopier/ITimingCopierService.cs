using Mapping_Tools.Core.Tools.TimingCopier;

namespace Mapping_Tools.Application.Tools.TimingCopier;

/// <summary>
///     Loads source and target beatmaps, applies Timing Copier, and saves each target.
/// </summary>
public interface ITimingCopierService
{
    /// <summary>
    ///     Copies source timing to every vertical-bar-separated target in the options.
    /// </summary>
    /// <param name="options">The source, targets, resnapping mode, and beat divisors.</param>
    /// <param name="progress">Receives aggregate completion after each target.</param>
    /// <param name="cancellationToken">Cancels loading, transformation, backup, or saving.</param>
    /// <returns>The target paths successfully processed before completion.</returns>
    Task<TimingCopierResult> CopyAsync(
        TimingCopierOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

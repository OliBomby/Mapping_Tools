using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;

namespace Mapping_Tools.Application.Tools.HitsoundCopier;

/// <summary>Copies hitsounds through the shared editing gateway.</summary>
public interface IHitsoundCopierService
{
    /// <summary>Copies source hitsounds to each vertical-bar-separated target path.</summary>
    /// <param name="options">The complete source, selection, matching, and filter state.</param>
    /// <param name="progress">Reports aggregate target completion.</param>
    /// <param name="cancellationToken">Cancels loading, transformation, backup, or saving.</param>
    /// <returns>The target paths and change summary.</returns>
    Task<HitsoundCopierResult> CopyAsync(
        HitsoundCopierOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

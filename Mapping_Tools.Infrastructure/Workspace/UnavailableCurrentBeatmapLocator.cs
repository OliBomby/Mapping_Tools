using Mapping_Tools.ApplicationServices.Workspace;

namespace Mapping_Tools.Infrastructure.Workspace;

/// <summary>
/// Leaves live osu! lookup explicitly unavailable until Wave 2 step 12 adds
/// the Windows Editor Reader/process-memory adapter.
/// </summary>
public sealed class UnavailableCurrentBeatmapLocator : ICurrentBeatmapLocator
{
    /// <inheritdoc/>
    public Task<string?> FindCurrentBeatmapAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(null);
    }
}

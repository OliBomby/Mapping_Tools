using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Reports that no current osu! beatmap can be located on an unsupported
///     editor platform.
/// </summary>
public sealed class UnsupportedPlatformCurrentBeatmapLocator : ICurrentBeatmapLocator
{
    /// <inheritdoc />
    public Task<string> FindCurrentBeatmapAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<string>(new InvalidOperationException(
            "Current osu! beatmap lookup is unavailable on this platform."));
    }
}

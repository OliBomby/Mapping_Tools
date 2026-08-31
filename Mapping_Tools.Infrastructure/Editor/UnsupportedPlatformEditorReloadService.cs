using Mapping_Tools.Application.BeatmapEditing.Contracts;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Completes editor-reload requests without sending platform-specific input
///     on unsupported operating systems.
/// </summary>
public sealed class UnsupportedPlatformEditorReloadService : IEditorReloadService
{
    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

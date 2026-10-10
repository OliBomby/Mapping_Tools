using Mapping_Tools.Application.BeatmapEditing.Contracts;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc;

/// <summary>Reloads the osu! editor through MTIPC.</summary>
public sealed class MtipcEditorReloadService : IEditorReloadService
{
    private readonly MtipcClient client = new();

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken = default) =>
        await Task.Run(() => client.ReloadEditor(cancellationToken), cancellationToken);
}

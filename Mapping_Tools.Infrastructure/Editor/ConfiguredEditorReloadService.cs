using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Selects exactly one configured editor-reload backend.
/// </summary>
public sealed class ConfiguredEditorReloadService : IEditorReloadService
{
    private readonly IEditorReloadService mtipcReload;
    private readonly IEditorReloadService simulatedKeypressReload;
    private readonly ApplicationSettings settings;

    /// <summary>Initializes an editor-reload backend selector.</summary>
    public ConfiguredEditorReloadService(
        ApplicationSettings settings,
        IEditorReloadService simulatedKeypressReload,
        IEditorReloadService mtipcReload)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.simulatedKeypressReload = simulatedKeypressReload
                                       ?? throw new ArgumentNullException(nameof(simulatedKeypressReload));
        this.mtipcReload = mtipcReload ?? throw new ArgumentNullException(nameof(mtipcReload));
    }

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return settings.EditorReload switch
        {
            EditorReloadMode.Disabled => Task.CompletedTask,
            EditorReloadMode.Mtipc => mtipcReload.ReloadAsync(cancellationToken),
            _ => simulatedKeypressReload.ReloadAsync(cancellationToken),
        };
    }
}

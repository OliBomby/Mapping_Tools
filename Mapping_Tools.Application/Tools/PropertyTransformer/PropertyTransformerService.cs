using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.PropertyTransformer;

namespace Mapping_Tools.Application.Tools.PropertyTransformer;

/// <summary>
///     Coordinates live-aware beatmap loading, transformation, backups, and persistence.
/// </summary>
public sealed class PropertyTransformerService : IPropertyTransformerService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly ApplicationSettings settings;

    /// <summary>
    ///     Creates the Property Transformer application service.
    /// </summary>
    /// <param name="editingGateway">Loads documents and saves them through the backup boundary.</param>
    /// <param name="settings">Supplies the automatic editor reload preference.</param>
    public PropertyTransformerService(
        IBeatmapEditingGateway editingGateway,
        ApplicationSettings settings)
    {
        this.editingGateway = editingGateway
                              ?? throw new ArgumentNullException(nameof(editingGateway));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public async Task<PropertyTransformerResult> TransformAsync(
        IReadOnlyList<string> paths,
        PropertyTransformerServiceOptions options,
        bool quickRun = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException(
                "Select at least one beatmap or storyboard.",
                nameof(paths));
        PropertyTransformerEngine.Validate(options);

        List<string> processedPaths = [];
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[index];
            var documentProgress = progress?.MapTo(index, paths.Count);

            if (Path.GetExtension(path).Equals(
                    ".osb",
                    StringComparison.OrdinalIgnoreCase))
            {
                var editor = await editingGateway
                    .OpenStoryboardAsync(path, cancellationToken)
                    .ConfigureAwait(false);
                PropertyTransformerEngine.Apply(
                    editor.StoryBoard,
                    options,
                    documentProgress,
                    cancellationToken);
                // Save the file
                await editingGateway.SaveAsync(
                        editor,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                var session = await editingGateway
                    .OpenBeatmapAsync(
                        path,
                        LiveBeatmapPreference.PreferLive,
                        cancellationToken)
                    .ConfigureAwait(false);
                PropertyTransformerEngine.Apply(
                    session.Beatmap,
                    options,
                    documentProgress,
                    cancellationToken);
                // Save the file
                await editingGateway.SaveAsync(
                        session,
                        AutomaticEditorReloadPolicy.ShouldReloadEditor(
                            session,
                            quickRun,
                            settings),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            processedPaths.Add(path);
        }

        progress?.Report(1);
        return new PropertyTransformerResult(processedPaths);
    }
}

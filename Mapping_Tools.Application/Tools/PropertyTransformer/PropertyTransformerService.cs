using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.Tools.PropertyTransformer;

namespace Mapping_Tools.Application.Tools.PropertyTransformer;

/// <summary>
///     Coordinates live-aware beatmap loading, transformation, backups, and persistence.
/// </summary>
public sealed class PropertyTransformerService : IPropertyTransformerService
{
    private readonly IBeatmapEditingGateway editingGateway;

    /// <summary>
    ///     Creates the Property Transformer application service.
    /// </summary>
    /// <param name="editingGateway">Loads documents and saves them through the backup boundary.</param>
    public PropertyTransformerService(IBeatmapEditingGateway editingGateway)
    {
        this.editingGateway = editingGateway
                              ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc />
    public async Task<PropertyTransformerResult> TransformAsync(
        IReadOnlyList<string> paths,
        PropertyTransformerOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(options);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException(
                "Select at least one beatmap or storyboard.",
                nameof(paths));

        List<string> processedPaths = [];
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[index];
            int progressIndex = index;
            var documentProgress = progress is null
                ? null
                : new Progress<double>(value =>
                    progress.Report((progressIndex * 100 + value) / paths.Count));

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
                    session.Editor.Beatmap,
                    options,
                    documentProgress,
                    cancellationToken);
                // Save the file
                await editingGateway.SaveAsync(
                        session,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            processedPaths.Add(path);
        }

        progress?.Report(100);
        return new PropertyTransformerResult(processedPaths);
    }
}

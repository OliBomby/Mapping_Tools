using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.SamplePlugin;

/// <summary>
///     Applies the sample plugin's tag edit through Mapping Tools' shared edit gateway.
/// </summary>
public sealed class SampleToolService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly ApplicationSettings settings;

    /// <summary>
    ///     Creates the sample edit service.
    /// </summary>
    /// <param name="editingGateway">Loads and saves beatmaps with backup protection.</param>
    /// <param name="settings">Supplies the automatic editor reload preference.</param>
    public SampleToolService(
        IBeatmapEditingGateway editingGateway,
        ApplicationSettings settings)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    internal async Task<int> AddTagAsync(
        IReadOnlyList<string> paths,
        string tag,
        bool quickRun = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        if (paths.Count == 0 || paths.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Select at least one beatmap.", nameof(paths));

        int changedCount = 0;
        for (int index = 0; index < paths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeatmapEditingSession session = await editingGateway
                .OpenBeatmapAsync(
                    paths[index],
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);

            StringValue tags = session.Beatmap.Metadata.TryGetValue("Tags", out StringValue? currentTags)
                ? currentTags
                : new StringValue(string.Empty);
            string[] existingTags = tags.Value
                .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (!existingTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                session.Beatmap.Metadata["Tags"] = new StringValue(
                    string.Join(' ', existingTags.Append(tag)));
                await editingGateway
                    .SaveAsync(
                        session,
                        AutomaticEditorReloadPolicy.ShouldReloadEditor(
                            session,
                            quickRun,
                            settings),
                        cancellationToken)
                    .ConfigureAwait(false);
                changedCount++;
            }

            progress?.Report((index + 1d) / paths.Count);
        }

        return changedCount;
    }
}

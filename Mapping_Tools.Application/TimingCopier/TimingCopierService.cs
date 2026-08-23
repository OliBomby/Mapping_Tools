using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.TimingCopier;

namespace Mapping_Tools.Application.TimingCopier;

/// <summary>
///     Coordinates live-aware source and target loading, transformation, backups, and persistence.
/// </summary>
public sealed class TimingCopierService : ITimingCopierService
{
    private readonly IBeatmapEditingGateway _editingGateway;

    /// <summary>
    ///     Creates the Timing Copier application service.
    /// </summary>
    /// <param name="editingGateway">Loads documents and saves them through the backup boundary.</param>
    public TimingCopierService(IBeatmapEditingGateway editingGateway)
    {
        _editingGateway = editingGateway
                          ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc />
    public async Task<TimingCopierResult> CopyAsync(
        TimingCopierOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ImportPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ExportPath);
        if (options.BeatDivisors is null || options.BeatDivisors.Length == 0 || options.BeatDivisors.Any(divisor => divisor is null))
            throw new ArgumentException(
                "Timing Copier requires at least one beat divisor.",
                nameof(options));

        if (options.ResnapMode is not TimingCopierResnapModes.PreserveBeatSpacing and
            not TimingCopierResnapModes.Resnap and
            not TimingCopierResnapModes.KeepObjectsFixed)
            throw new ArgumentException(
                "Timing Copier received an unknown resnapping mode.",
                nameof(options));

        string[] targetPaths = options.ExportPath
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (targetPaths.Length == 0)
            throw new ArgumentException(
                "Select at least one target beatmap.",
                nameof(options));

        var source = await _editingGateway
            .OpenBeatmapAsync(
                options.ImportPath,
                LiveBeatmapPreference.PreferLive,
                cancellationToken)
            .ConfigureAwait(false);

        List<string> processedPaths = [];
        for (int index = 0; index < targetPaths.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string targetPath = targetPaths[index];

            var target = await _editingGateway
                .OpenBeatmapAsync(
                    targetPath,
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);
            TimingCopierEngine.Apply(
                target.Editor.Beatmap,
                source.Editor.Beatmap,
                options,
                cancellationToken);
            // Save the file
            await _editingGateway
                .SaveAsync(target, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(targetPath);
            progress?.Report((index + 1) * 100d / targetPaths.Length);
        }

        return new TimingCopierResult(processedPaths);
    }
}

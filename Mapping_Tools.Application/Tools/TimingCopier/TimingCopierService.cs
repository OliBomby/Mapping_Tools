using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.TimingCopier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Application.Tools.TimingCopier;

/// <summary>
///     Coordinates live-aware source and target loading, transformation, backups, and persistence.
/// </summary>
public sealed class TimingCopierService : ITimingCopierService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly ILogger<TimingCopierService> logger;

    /// <summary>
    ///     Creates the Timing Copier application service.
    /// </summary>
    /// <param name="editingGateway">Loads documents and saves them through the backup boundary.</param>
    /// <param name="logger">Records source and target copy milestones.</param>
    public TimingCopierService(IBeatmapEditingGateway editingGateway, ILogger<TimingCopierService>? logger = null)
    {
        this.editingGateway = editingGateway
                              ?? throw new ArgumentNullException(nameof(editingGateway));
        this.logger = logger ?? NullLogger<TimingCopierService>.Instance;
    }

    /// <inheritdoc />
    public async Task<TimingCopierResult> CopyAsync(
        TimingCopierServiceOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Validate(options);

        string[] targetPaths = options.ExportPath
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (targetPaths.Length == 0)
            throw new ArgumentException(
                ApplicationStrings.Tools_TargetBeatmapRequired,
                nameof(options));
        logger.LogInformation("Started from {SourcePath} for {TargetCount} targets", options.ImportPath, targetPaths.Length);

        var source = await editingGateway
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
            logger.LogInformation("Processing target {Index}/{Count}: {Path}", index + 1, targetPaths.Length, targetPath);

            var target = await editingGateway
                .OpenBeatmapAsync(
                    targetPath,
                    LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);
            TimingCopierEngine.Apply(
                target.Beatmap,
                source.Beatmap,
                options,
                cancellationToken);
            logger.LogInformation("Applied timing to {Path}; saving", targetPath);
            // Save the file
            await editingGateway
                .SaveAsync(target, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            processedPaths.Add(targetPath);
            progress?.Report(index + 1, targetPaths.Length);
        }

        logger.LogInformation("Completed {Count} targets", processedPaths.Count);
        return new TimingCopierResult(processedPaths);
    }

    private static void Validate(TimingCopierServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ImportPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ExportPath);
        TimingCopierEngine.Validate(options);
    }
}

using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.AutoFail;
using Mapping_Tools.Core.Tools.AutoFail.Models;

namespace Mapping_Tools.Application.Tools.AutoFail;

/// <summary>Coordinates live-aware analysis and backup-before-write auto-fail fixes.</summary>
public sealed class AutoFailService : IAutoFailService
{
    private readonly IBeatmapEditingGateway editingGateway;

    /// <summary>Creates a service that opens and saves beatmaps through the shared editing gateway.</summary>
    /// <param name="editingGateway">The live-aware, backup-before-write beatmap gateway.</param>
    public AutoFailService(IBeatmapEditingGateway editingGateway)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc />
    public async Task<AutoFailRun> AnalyzeAsync(
        AutoFailServiceOptions options,
        CancellationToken cancellationToken = default)
    {
        Validate(options);
        var session = await editingGateway.OpenBeatmapAsync(
            options.Path,
            LiveBeatmapPreference.PreferLive,
            cancellationToken).ConfigureAwait(false);
        var beatmap = session.Editor.Beatmap;
        double approachRate = options.ApproachRateOverride < 0
            ? beatmap.Difficulty["ApproachRate"].DoubleValue
            : options.ApproachRateOverride;
        double overallDifficulty = options.OverallDifficultyOverride < 0
            ? beatmap.Difficulty["OverallDifficulty"].DoubleValue
            : options.OverallDifficultyOverride;
        // Get approach time and radius of the 50 score hit window
        AutoFailDetectorEngine detector = new(
            beatmap.HitObjects,
            (int)beatmap.GetMapStartTime(),
            (int)beatmap.GetMapEndTime(),
            (int)beatmap.GetAutoFailCheckTime(),
            (int)Beatmap.GetApproachTime(approachRate),
            (int)Math.Ceiling(200 - 10 * overallDifficulty),
            options.PhysicsUpdateLeniency);
        // Detect auto-fail
        var analysis = detector.Analyze(cancellationToken);
        return new AutoFailRun(analysis, beatmap.GetMapEndTime(), session, detector);
    }

    /// <inheritdoc />
    public IEnumerable<AutoFailFixPlan> GetFixPlans(
        AutoFailRun run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        return (run.Detector ?? throw new InvalidOperationException("This analysis has no fix-planning session."))
            .GetFixPlans(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ApplyFixAsync(
        AutoFailRun run,
        AutoFailFixPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(plan);
        cancellationToken.ThrowIfCancellationRequested();
        var detector = run.Detector ?? throw new InvalidOperationException("This analysis has no fix-planning session.");
        var session = run.Session ?? throw new InvalidOperationException("This analysis has no editing session.");
        // Fix auto-fail
        detector.ApplyFix(plan);
        await editingGateway.SaveAsync(
            session,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static void Validate(AutoFailServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Path);
        if (!double.IsFinite(options.ApproachRateOverride) || options.ApproachRateOverride < -1 || options.ApproachRateOverride > 10)
            throw new ArgumentOutOfRangeException(nameof(options.ApproachRateOverride));
        if (!double.IsFinite(options.OverallDifficultyOverride) || options.OverallDifficultyOverride < -1 || options.OverallDifficultyOverride > 10)
            throw new ArgumentOutOfRangeException(nameof(options.OverallDifficultyOverride));
    }
}

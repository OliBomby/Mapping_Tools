using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.AutoFail;

namespace Mapping_Tools.Application.AutoFail;

/// <summary>Coordinates live-aware analysis and backup-before-write auto-fail fixes.</summary>
public sealed class AutoFailService : IAutoFailService
{
    private readonly IBeatmapEditingGateway _editingGateway;

    /// <summary>Creates a service that opens and saves beatmaps through the shared editing gateway.</summary>
    /// <param name="editingGateway">The live-aware, backup-before-write beatmap gateway.</param>
    public AutoFailService(IBeatmapEditingGateway editingGateway) =>
        _editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));

    /// <inheritdoc/>
    public async Task<AutoFailRun> AnalyzeAsync(
        AutoFailOptions options,
        CancellationToken cancellationToken = default)
    {
        Validate(options);
        BeatmapEditingSession session = await _editingGateway.OpenBeatmapAsync(
            options.Path,
            LiveBeatmapPreference.PreferLive,
            cancellationToken).ConfigureAwait(false);
        Beatmap beatmap = session.Editor.Beatmap;
        double approachRate = options.ApproachRateOverride < 0
            ? beatmap.Difficulty["ApproachRate"].DoubleValue
            : options.ApproachRateOverride;
        double overallDifficulty = options.OverallDifficultyOverride < 0
            ? beatmap.Difficulty["OverallDifficulty"].DoubleValue
            : options.OverallDifficultyOverride;
        AutoFailDetectorEngine detector = new(
            beatmap.HitObjects,
            (int)beatmap.GetMapStartTime(),
            (int)beatmap.GetMapEndTime(),
            (int)beatmap.GetAutoFailCheckTime(),
            (int)Beatmap.GetApproachTime(approachRate),
            (int)Math.Ceiling(200 - 10 * overallDifficulty),
            options.PhysicsUpdateLeniency);
        AutoFailAnalysis analysis = detector.Analyze(cancellationToken);
        return new AutoFailRun(analysis, beatmap.GetMapEndTime(), session, detector);
    }

    /// <inheritdoc/>
    public IEnumerable<AutoFailFixPlan> GetFixPlans(
        AutoFailRun run,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        return (run.Detector ?? throw new InvalidOperationException("This analysis has no fix-planning session."))
            .GetFixPlans(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ApplyFixAsync(
        AutoFailRun run,
        AutoFailFixPlan plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(plan);
        cancellationToken.ThrowIfCancellationRequested();
        AutoFailDetectorEngine detector = run.Detector ??
            throw new InvalidOperationException("This analysis has no fix-planning session.");
        BeatmapEditingSession session = run.Session ??
            throw new InvalidOperationException("This analysis has no editing session.");
        detector.ApplyFix(plan);
        await _editingGateway.SaveAsync(
            session.Editor,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static void Validate(AutoFailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Path);
        ValidateDifficulty(options.ApproachRateOverride, nameof(options.ApproachRateOverride));
        ValidateDifficulty(options.OverallDifficultyOverride, nameof(options.OverallDifficultyOverride));
        if (options.PhysicsUpdateLeniency < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.PhysicsUpdateLeniency));
        }
    }

    private static void ValidateDifficulty(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < -1 || value > 10)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

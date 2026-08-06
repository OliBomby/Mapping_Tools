using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.AutoFail;

namespace Mapping_Tools.Application.AutoFail;

public sealed record AutoFailOptions(
    string Path,
    double ApproachRateOverride = -1,
    double OverallDifficultyOverride = -1,
    int PhysicsUpdateLeniency = 9);

public sealed class AutoFailRun
{
    public AutoFailRun(AutoFailAnalysis analysis, double mapEndTime)
    {
        Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        MapEndTime = mapEndTime;
    }

    internal AutoFailRun(
        AutoFailAnalysis analysis,
        double mapEndTime,
        BeatmapEditingSession session,
        AutoFailDetectorEngine detector)
    {
        Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        MapEndTime = mapEndTime;
        Session = session;
        Detector = detector;
    }

    public AutoFailAnalysis Analysis { get; }
    public double MapEndTime { get; }
    internal BeatmapEditingSession? Session { get; }
    internal AutoFailDetectorEngine? Detector { get; }
}

public interface IAutoFailService
{
    Task<AutoFailRun> AnalyzeAsync(
        AutoFailOptions options,
        CancellationToken cancellationToken = default);

    IEnumerable<AutoFailFixPlan> GetFixPlans(
        AutoFailRun run,
        CancellationToken cancellationToken = default);

    Task ApplyFixAsync(
        AutoFailRun run,
        AutoFailFixPlan plan,
        CancellationToken cancellationToken = default);
}

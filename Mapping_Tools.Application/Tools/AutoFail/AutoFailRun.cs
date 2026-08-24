using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.AutoFail;

namespace Mapping_Tools.Application.Tools.AutoFail;

/// <summary>Contains the analysis result and retained edit state for a possible fix operation.</summary>
public sealed class AutoFailRun
{
    /// <summary>Creates a detached result that cannot apply fixes to a beatmap.</summary>
    /// <param name="analysis">The detected unloading objects and candidate fixes.</param>
    /// <param name="mapEndTime">The final timeline position in milliseconds.</param>
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

    /// <summary>Gets the detected unloading objects and candidate fixes.</summary>
    public AutoFailAnalysis Analysis { get; }

    /// <summary>Gets the final beatmap timeline position in milliseconds.</summary>
    public double MapEndTime { get; }

    internal BeatmapEditingSession? Session { get; }
    internal AutoFailDetectorEngine? Detector { get; }
}


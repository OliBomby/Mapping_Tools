using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.TimingHelper;

/// <summary>Provides the discoverable metadata for Timing Helper.</summary>
public static class TimingHelperToolDefinition
{
    /// <summary>Gets the stable Timing Helper metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "timing-helper",
        "Timing Helper",
        "Adjust BPM and add redlines so selected markers become snapped.",
        ["timing", "redlines", "BPM", "markers", "beat divisors"],
        QuickRunTargets.Always);
}

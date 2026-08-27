using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.AutoFail;

/// <summary>Provides the discoverable metadata for Auto-fail Detector.</summary>
public static class AutoFailDetectorToolDefinition
{
    /// <summary>Gets the stable Auto-fail Detector metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "auto-fail-detector",
        "Auto-fail Detector",
        "Detect incorrect object loading in overlapping patterns.",
        ["auto fail", "2b", "unloading", "objects"],
        QuickRunTargets.Always);
}

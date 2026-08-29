using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Tools;

namespace Mapping_Tools.SamplePlugin;

/// <summary>
///     Provides the discoverable metadata for the sample plugin tool.
/// </summary>
public static class SampleToolDefinition
{
    /// <summary>
    ///     Gets the stable sample tool metadata used by shell and QuickRun catalogs.
    /// </summary>
    public static ToolDefinition Definition { get; } = new(
        "sample-plugin",
        "Sample Plugin",
        "A single-run tool that adds a tag to selected beatmaps.",
        ["sample", "plugin", "example", "tag"],
        QuickRunTargets.Always);
}

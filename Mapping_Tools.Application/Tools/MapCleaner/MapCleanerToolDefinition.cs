using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.Tools.MapCleaner;

/// <summary>Provides the discoverable metadata for Map Cleaner.</summary>
public static class MapCleanerToolDefinition
{
    /// <summary>Gets the stable Map Cleaner metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "map-cleaner",
        "Map Cleaner",
        "Rebuild useful greenlines and optionally resnap map content.",
        ["clean", "greenline", "resnap", "samples"],
        QuickRunTargets.Always);
}

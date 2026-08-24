using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.MapCleaner;

namespace Mapping_Tools.Application.Tools.MapCleaner;

/// <summary>Persists the configurable Map Cleaner options in a project file.</summary>
public sealed class MapCleanerProject
{
    /// <summary>Gets or sets the cleanup options stored by the project.</summary>
    public MapCleanerOptions MapCleanerArgs { get; set; } = new();
}


using Mapping_Tools.Core.Tools.MapCleaner.Models;

namespace Mapping_Tools.Application.Tools.MapCleaner;

/// <summary>Persists the configurable Map Cleaner options in a project file.</summary>
public sealed class MapCleanerProject
{
    /// <summary>Gets or sets the cleanup options stored by the project.</summary>
    public MapCleanerProjectOptions MapCleanerArgs { get; set; } = new();

    /// <summary>Contains Core cleanup inputs and application-owned sample deletion state.</summary>
    public sealed class MapCleanerProjectOptions : MapCleanerOptions
    {
        /// <summary>Gets or sets whether unused samples are moved to recoverable storage.</summary>
        public bool RemoveUnusedSamples { get; set; }

    }
}

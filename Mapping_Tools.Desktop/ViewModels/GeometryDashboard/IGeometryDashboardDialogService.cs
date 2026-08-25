using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Desktop.Models;

namespace Mapping_Tools.Desktop.ViewModels.GeometryDashboard;

/// <summary>Provides the window interactions owned by the Geometry Dashboard.</summary>
public interface IGeometryDashboardDialogService
{
    /// <summary>Shows preferences and returns an accepted clone, or null on cancel.</summary>
    Task<SnappingToolsPreferences?> ShowPreferencesAsync(SnappingToolsPreferences preferences);

    /// <summary>Shows the modeless save-slot editor.</summary>
    /// <param name="project">The project whose slots are edited.</param>
    /// <param name="loadSlot">Loads one slot into the active dashboard.</param>
    /// <param name="refreshHotkeys">Refreshes the active save-slot shortcut registrations.</param>
    Task ShowProjectSlotsAsync(
        SnappingToolsProject project,
        Action<SnappingToolsSaveSlot> loadSlot,
        Action refreshHotkeys);

    /// <summary>Shows generator-specific settings and returns whether Apply was pressed.</summary>
    Task<bool> ShowGeneratorSettingsAsync(GeneratorSettings settings);
}


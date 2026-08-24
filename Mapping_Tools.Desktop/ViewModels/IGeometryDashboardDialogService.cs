using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Desktop.Shell;
using Material.Icons;

namespace Mapping_Tools.Desktop.ViewModels;

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


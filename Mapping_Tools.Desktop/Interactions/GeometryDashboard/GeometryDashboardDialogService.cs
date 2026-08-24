using Avalonia.Controls;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Desktop.ViewModels.GeometryDashboard;
using GeometryDashboardGeneratorSettingsWindow = Mapping_Tools.Desktop.Views.GeometryDashboard.GeometryDashboardGeneratorSettingsWindow;
using GeometryDashboardPreferencesWindow = Mapping_Tools.Desktop.Views.GeometryDashboard.GeometryDashboardPreferencesWindow;
using GeometryDashboardProjectWindow = Mapping_Tools.Desktop.Views.GeometryDashboard.GeometryDashboardProjectWindow;

namespace Mapping_Tools.Desktop.Interactions.GeometryDashboard;

/// <summary>Creates Geometry Dashboard dialogs without passing Window types to its view model.</summary>
public sealed class GeometryDashboardDialogService : IGeometryDashboardDialogService
{
    private readonly Func<Window> owner;

    /// <summary>Creates a dialog service owned by the shell window.</summary>
    /// <param name="owner">Returns the current shell window.</param>
    public GeometryDashboardDialogService(Func<Window> owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <inheritdoc />
    public async Task<SnappingToolsPreferences?> ShowPreferencesAsync(SnappingToolsPreferences preferences)
    {
        GeometryDashboardPreferencesDialogViewModel viewModel = new(preferences);
        GeometryDashboardPreferencesWindow window = new() { DataContext = viewModel };
        viewModel.Close = result => window.Close(result);
        return await window.ShowDialog<SnappingToolsPreferences?>(owner());
    }

    /// <inheritdoc />
    public async Task ShowProjectSlotsAsync(
        SnappingToolsProject project,
        Action<SnappingToolsSaveSlot> loadSlot,
        Action refreshHotkeys)
    {
        GeometryDashboardProjectSlotsViewModel viewModel = new(project, loadSlot, refreshHotkeys);
        GeometryDashboardProjectWindow window = new() { DataContext = viewModel };
        viewModel.Close = window.Close;
        window.Show(owner());
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> ShowGeneratorSettingsAsync(GeneratorSettings settings)
    {
        GeometryDashboardGeneratorSettingsDialogViewModel viewModel = new(settings);
        GeometryDashboardGeneratorSettingsWindow window = new() { DataContext = viewModel };
        viewModel.Close = result => window.Close(result);
        object? result = await window.ShowDialog<object?>(owner());
        return result is true;
    }
}


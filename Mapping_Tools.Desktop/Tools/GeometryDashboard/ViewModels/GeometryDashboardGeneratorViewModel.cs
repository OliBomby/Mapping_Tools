using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

/// <summary>Wraps one Core generator for compiled Avalonia bindings.</summary>
public sealed partial class GeometryDashboardGeneratorViewModel : ObservableObject
{
    private readonly GeometryDashboardViewModel owner;

    /// <summary>Creates a generator row.</summary>
    public GeometryDashboardGeneratorViewModel(RelevantObjectsGenerator model, GeometryDashboardViewModel owner)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>Gets the Core generator.</summary>
    public RelevantObjectsGenerator Model { get; }

    /// <summary>Gets the display name.</summary>
    public string Name => Model.Name;

    /// <summary>Gets the tooltip text.</summary>
    public string Tooltip => Model.Tooltip;

    /// <summary>Gets the settings object shown in the row.</summary>
    public GeneratorSettings Settings => Model.Settings;

    /// <summary>Shows this generator's settings dialog.</summary>
    [RelayCommand]
    private Task OpenSettingsAsync()
    {
        return owner.ShowGeneratorSettingsAsync(this);
    }
}


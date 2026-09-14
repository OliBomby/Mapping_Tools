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
    public string Tooltip => Model.Description;

    /// <summary>Gets the settings object shown in the row.</summary>
    public GeneratorSettings Settings => Model.Settings;

    /// <summary>Gets or sets whether this generator participates in calculation.</summary>
    public bool IsActive
    {
        get => Settings.IsActive;
        set
        {
            if (Settings.IsActive == value) return;
            Settings.IsActive = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets whether inputs must be selected in sequence.</summary>
    public bool IsSequential
    {
        get => Settings.IsSequential;
        set
        {
            if (Settings.IsSequential == value) return;
            Settings.IsSequential = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the multiplier applied to parent relevance.</summary>
    public double RelevancyRatio
    {
        get => Settings.RelevancyRatio;
        set
        {
            if (Settings.RelevancyRatio.Equals(value)) return;
            Settings.RelevancyRatio = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Notifies bindings that the underlying generator settings were updated.</summary>
    public void NotifySettingsChanged()
    {
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(IsSequential));
        OnPropertyChanged(nameof(RelevancyRatio));
    }

    /// <summary>Shows this generator's settings dialog.</summary>
    [RelayCommand]
    private Task OpenSettingsAsync()
    {
        return owner.ShowGeneratorSettingsAsync(this);
    }
}

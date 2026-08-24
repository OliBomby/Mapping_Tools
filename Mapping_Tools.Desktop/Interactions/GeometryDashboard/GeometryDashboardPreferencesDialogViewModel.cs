using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Desktop.Interactions.GeometryDashboard;

/// <summary>Edits a cloned preference document until Apply or Cancel is selected.</summary>
public sealed partial class GeometryDashboardPreferencesDialogViewModel : ObservableObject
{
    /// <summary>Creates the dialog over an independent preference clone.</summary>
    public GeometryDashboardPreferencesDialogViewModel(SnappingToolsPreferences preferences)
    {
        Preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        Appearance = new ObservableCollection<GeometryDashboardPreferenceRowViewModel>(
            Preferences.RelevantObjectPreferences.Values.Select(value => new GeometryDashboardPreferenceRowViewModel(value)));
    }

    /// <summary>Gets the independent document being edited.</summary>
    public SnappingToolsPreferences Preferences { get; }

    /// <summary>Gets the supported root-hit-object selection modes.</summary>
    public IReadOnlyList<SelectedHitObjectMode> SelectedHitObjectModes { get; } = Enum.GetValues<SelectedHitObjectMode>();

    /// <summary>Gets the supported view modes offered by the compact dialog.</summary>
    public IReadOnlyList<ViewMode> ViewModes { get; } = Enum.GetValues<ViewMode>();

    /// <summary>Gets the supported graph-refresh modes.</summary>
    public IReadOnlyList<UpdateMode> UpdateModes { get; } = Enum.GetValues<UpdateMode>();

    /// <summary>Gets the editable appearance groups retained by the project.</summary>
    public ObservableCollection<GeometryDashboardPreferenceRowViewModel> Appearance { get; }

    /// <summary>Gets or sets the complete graph while the snap key is down.</summary>
    public bool KeyDownEverything { get => Preferences.KeyDownViewMode.HasFlag(ViewMode.Everything); set => SetViewFlag(ViewMode.Everything, value); }

    /// <summary>Gets or sets the parent graph while the snap key is down.</summary>
    public bool KeyDownParents { get => Preferences.KeyDownViewMode.HasFlag(ViewMode.Parents); set => SetViewFlag(ViewMode.Parents, value); }

    /// <summary>Gets or sets direct parents while the snap key is down.</summary>
    public bool KeyDownDirectParents { get => Preferences.KeyDownViewMode.HasFlag(ViewMode.DirectParents); set => SetViewFlag(ViewMode.DirectParents, value); }

    /// <summary>Gets or sets the child graph while the snap key is down.</summary>
    public bool KeyDownChildren { get => Preferences.KeyDownViewMode.HasFlag(ViewMode.Children); set => SetViewFlag(ViewMode.Children, value); }

    /// <summary>Gets or sets direct children while the snap key is down.</summary>
    public bool KeyDownDirectChildren { get => Preferences.KeyDownViewMode.HasFlag(ViewMode.DirectChildren); set => SetViewFlag(ViewMode.DirectChildren, value); }

    /// <summary>Gets or sets whether key-up mode shows the complete graph.</summary>
    public bool KeyUpEverything
    {
        get => Preferences.KeyUpViewMode.HasFlag(ViewMode.Everything);
        set => SetKeyUpMode(value ? ViewMode.Everything : ViewMode.Nothing);
    }

    /// <summary>Gets or sets whether key-up mode hides all generated objects.</summary>
    public bool KeyUpNothing
    {
        get => Preferences.KeyUpViewMode == ViewMode.Nothing;
        set
        {
            if (value) SetKeyUpMode(ViewMode.Nothing);
        }
    }

    /// <summary>Gets or sets whether all hit objects are always shown.</summary>
    public bool SelectedAllVisible
    {
        get => Preferences.SelectedHitObjectMode == SelectedHitObjectMode.AllwaysAllVisible;
        set
        {
            if (value) SetSelectedMode(SelectedHitObjectMode.AllwaysAllVisible);
        }
    }

    /// <summary>Gets or sets whether visible objects or selected objects are shown.</summary>
    public bool SelectedVisibleOrSelected
    {
        get => Preferences.SelectedHitObjectMode == SelectedHitObjectMode.VisibleOrSelected;
        set
        {
            if (value) SetSelectedMode(SelectedHitObjectMode.VisibleOrSelected);
        }
    }

    /// <summary>Gets or sets whether only selected hit objects are shown.</summary>
    public bool SelectedOnly
    {
        get => Preferences.SelectedHitObjectMode == SelectedHitObjectMode.OnlySelected;
        set
        {
            if (value) SetSelectedMode(SelectedHitObjectMode.OnlySelected);
        }
    }

    /// <summary>Gets or sets whether every editor change refreshes the graph.</summary>
    public bool UpdatingAnyChange
    {
        get => Preferences.UpdateMode == UpdateMode.AnyChange;
        set
        {
            if (value) SetUpdateMode(UpdateMode.AnyChange);
        }
    }

    /// <summary>Gets or sets whether editor time changes refresh the graph.</summary>
    public bool UpdatingTimeChange
    {
        get => Preferences.UpdateMode == UpdateMode.TimeChange;
        set
        {
            if (value) SetUpdateMode(UpdateMode.TimeChange);
        }
    }

    /// <summary>Gets or sets whether the refresh hotkey refreshes the graph.</summary>
    public bool UpdatingHotkeyDown
    {
        get => Preferences.UpdateMode == UpdateMode.HotkeyDown;
        set
        {
            if (value) SetUpdateMode(UpdateMode.HotkeyDown);
        }
    }

    /// <summary>Gets or sets whether activating osu! refreshes the graph.</summary>
    public bool UpdatingOsuActivated
    {
        get => Preferences.UpdateMode == UpdateMode.OsuActivated;
        set
        {
            if (value) SetUpdateMode(UpdateMode.OsuActivated);
        }
    }

    /// <summary>Receives the window close callback.</summary>
    public Action<SnappingToolsPreferences?>? Close { get; set; }

    /// <summary>Applies the clone to the caller.</summary>
    [RelayCommand]
    private void Apply()
    {
        Close?.Invoke(Preferences);
    }

    /// <summary>Discards the clone.</summary>
    [RelayCommand]
    private void Cancel()
    {
        Close?.Invoke(null);
    }

    private void SetViewFlag(ViewMode flag, bool enabled)
    {
        Preferences.KeyDownViewMode = enabled
            ? Preferences.KeyDownViewMode | flag
            : Preferences.KeyDownViewMode & ~flag;
        OnPropertyChanged(nameof(KeyDownEverything));
        OnPropertyChanged(nameof(KeyDownParents));
        OnPropertyChanged(nameof(KeyDownDirectParents));
        OnPropertyChanged(nameof(KeyDownChildren));
        OnPropertyChanged(nameof(KeyDownDirectChildren));
        OnPropertyChanged(nameof(KeyUpEverything));
        OnPropertyChanged(nameof(KeyUpNothing));
    }

    private void SetKeyUpMode(ViewMode mode)
    {
        Preferences.KeyUpViewMode = mode;
        OnPropertyChanged(nameof(KeyUpEverything));
        OnPropertyChanged(nameof(KeyUpNothing));
    }

    private void SetSelectedMode(SelectedHitObjectMode mode)
    {
        Preferences.SelectedHitObjectMode = mode;
        OnPropertyChanged(nameof(SelectedAllVisible));
        OnPropertyChanged(nameof(SelectedVisibleOrSelected));
        OnPropertyChanged(nameof(SelectedOnly));
    }

    private void SetUpdateMode(UpdateMode mode)
    {
        Preferences.UpdateMode = mode;
        OnPropertyChanged(nameof(UpdatingAnyChange));
        OnPropertyChanged(nameof(UpdatingTimeChange));
        OnPropertyChanged(nameof(UpdatingHotkeyDown));
        OnPropertyChanged(nameof(UpdatingOsuActivated));
    }
}


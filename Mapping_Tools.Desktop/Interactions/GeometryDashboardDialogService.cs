using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;

namespace Mapping_Tools.Desktop.Interactions;

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

/// <summary>Edits one neutral geometry appearance group.</summary>
public sealed class GeometryDashboardPreferenceRowViewModel : ObservableObject
{
    private string? pendingColorText;

    /// <summary>Creates a row over one cloned appearance group.</summary>
    public GeometryDashboardPreferenceRowViewModel(RelevantObjectPreferences preference)
    {
        Preference = preference;
    }

    /// <summary>Gets the stable preference-group label.</summary>
    public string Name => Preference.Name;

    /// <summary>Gets the Core appearance settings.</summary>
    public RelevantObjectPreferences Preference { get; }

    /// <summary>Gets or sets the color using Avalonia's color-picker type.</summary>
    public Color Color
    {
        get => Color.FromArgb(Preference.Color.A, Preference.Color.R, Preference.Color.G, Preference.Color.B);
        set
        {
            Preference.Color = RgbaColour.FromArgb(value.A, value.R, value.G, value.B);
            ColorTextError = null;
            pendingColorText = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorText));
            OnPropertyChanged(nameof(ColorTextError));
        }
    }

    /// <summary>Gets or sets the serialized color text.</summary>
    public string ColorText
    {
        get => pendingColorText ?? Preference.Color.ToString();
        set
        {
            string hex = (value ?? string.Empty).TrimStart('#');
            if (hex.Length == 6) hex = "FF" + hex;
            if (hex.Length == 8
                && byte.TryParse(hex[..2], NumberStyles.HexNumber, null, out byte a)
                && byte.TryParse(hex[2..4], NumberStyles.HexNumber, null, out byte r)
                && byte.TryParse(hex[4..6], NumberStyles.HexNumber, null, out byte g)
                && byte.TryParse(hex[6..8], NumberStyles.HexNumber, null, out byte b))
            {
                Preference.Color = RgbaColour.FromArgb(a, r, g, b);
                ColorTextError = null;
                pendingColorText = null;
            }
            else
            {
                ColorTextError = "Color format error.";
                pendingColorText = value;
            }

            OnPropertyChanged(nameof(Color));
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorTextError));
        }
    }

    /// <summary>Gets the validation message for invalid hexadecimal colour text.</summary>
    public string? ColorTextError { get; private set; }

    /// <summary>Gets or sets the opacity multiplier.</summary>
    public double Opacity
    {
        get => Preference.Opacity;
        set
        {
            Preference.Opacity = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the stroke thickness.</summary>
    public double Thickness
    {
        get => Preference.Thickness;
        set
        {
            Preference.Thickness = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the point size where supported.</summary>
    public double Size
    {
        get => Preference.Size;
        set
        {
            Preference.Size = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets whether point size applies to this group.</summary>
    public bool HasSizeOption => Preference.HasSizeOption;

    /// <summary>Gets or sets the dash pattern.</summary>
    public DashStylesEnum DashStyle
    {
        get => Preference.Dashstyle;
        set
        {
            Preference.Dashstyle = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets all available dash patterns.</summary>
    public IReadOnlyList<DashStylesEnum> DashStyles { get; } = Enum.GetValues<DashStylesEnum>();
}

/// <summary>Edits ordered Geometry Dashboard save slots.</summary>
public sealed partial class GeometryDashboardProjectSlotsViewModel : ObservableObject
{
    private readonly Action<SnappingToolsSaveSlot> loadSlot;
    private readonly Action refreshHotkeys;

    /// <summary>Creates the save-slot editor over the live project.</summary>
    public GeometryDashboardProjectSlotsViewModel(
        SnappingToolsProject project,
        Action<SnappingToolsSaveSlot> loadSlot,
        Action refreshHotkeys)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        this.loadSlot = loadSlot ?? throw new ArgumentNullException(nameof(loadSlot));
        this.refreshHotkeys = refreshHotkeys ?? throw new ArgumentNullException(nameof(refreshHotkeys));
    }

    /// <summary>Gets the live project slots.</summary>
    public SnappingToolsProject Project { get; }

    /// <summary>Gets or sets the selected slot.</summary>
    [ObservableProperty]
    public partial SnappingToolsSaveSlot? SelectedSlot { get; set; }

    /// <summary>Gets the extended-selection save slots currently selected in the list.</summary>
    public ObservableCollection<SnappingToolsSaveSlot> SelectedSlots { get; } = [];

    /// <summary>Receives the window close action.</summary>
    public Action? Close { get; set; }

    /// <summary>Replaces the extended list selection supplied by the Avalonia list control.</summary>
    /// <param name="slots">The selected live save slots.</param>
    public void SetSelectedSlots(IEnumerable<SnappingToolsSaveSlot> slots)
    {
        SelectedSlots.Clear();
        foreach (var slot in slots) SelectedSlots.Add(slot);
        SelectedSlot = SelectedSlots.LastOrDefault();
    }

    /// <summary>Adds a default slot after the existing slots.</summary>
    [RelayCommand]
    private void Add()
    {
        SnappingToolsSaveSlot slot;
        lock (Project)
        {
            slot = new SnappingToolsSaveSlot { Name = $"Save {Project.SaveSlots.Count + 1}" };
            Project.SaveToSlot(slot);
            Project.SaveSlots.Add(slot);
        }

        SelectedSlot = slot;
    }

    /// <summary>Removes the selected slot or the last slot.</summary>
    [RelayCommand]
    private void Remove()
    {
        var slots = SelectedSlots.Count > 0
            ? SelectedSlots.ToArray()
            : SelectedSlot is not null
                ? [SelectedSlot]
                : [];
        if (slots.Length == 0 && Project.SaveSlots.Count > 0) slots = [Project.SaveSlots[^1]];
        lock (Project)
        {
            foreach (var slot in slots) Project.SaveSlots.Remove(slot);
        }

        SelectedSlots.Clear();
        lock (Project)
        {
            SelectedSlot = Project.SaveSlots.LastOrDefault();
        }
    }

    /// <summary>Duplicates the selected slot using the legacy copy suffix.</summary>
    [RelayCommand]
    private void Duplicate()
    {
        var slots = SelectedSlots.Count > 0
            ? SelectedSlots.ToArray()
            : SelectedSlot is not null
                ? [SelectedSlot]
                : [];
        SnappingToolsSaveSlot? lastCopy = null;
        lock (Project)
        {
            foreach (var slot in slots)
            {
                var copy = (SnappingToolsSaveSlot)slot.Clone();
                copy.Name += " - Copy";
                Project.SaveSlots.Insert(Project.SaveSlots.IndexOf(slot) + 1, copy);
                lastCopy = copy;
            }
        }

        if (lastCopy is not null) SetSelectedSlots([lastCopy]);
    }

    /// <summary>Loads the selected slot into the active dashboard.</summary>
    [RelayCommand]
    private void Load(SnappingToolsSaveSlot? slot = null)
    {
        if (slot is not null) loadSlot(slot);
        else if (SelectedSlot is not null) loadSlot(SelectedSlot);
    }

    /// <summary>Saves current dashboard preferences into the selected slot.</summary>
    [RelayCommand]
    private void Save(SnappingToolsSaveSlot? slot = null)
    {
        lock (Project)
        {
            if (slot is not null) Project.SaveToSlot(slot);
            else if (SelectedSlot is not null) Project.SaveToSlot(SelectedSlot);
        }
    }

    /// <summary>Re-registers all save-slot hotkeys after editing their definitions.</summary>
    [RelayCommand]
    private void RefreshHotkeys()
    {
        refreshHotkeys();
    }
}

/// <summary>Reflects typed generator settings into a compact dialog row model.</summary>
public sealed partial class GeometryDashboardGeneratorSettingsDialogViewModel : ObservableObject
{
    /// <summary>Creates the dialog over an independent generator-settings clone.</summary>
    public GeometryDashboardGeneratorSettingsDialogViewModel(GeneratorSettings settings)
    {
        OriginalSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        Settings = (GeneratorSettings)settings.Clone();
        Rows = new ObservableCollection<GeometryDashboardGeneratorSettingRowViewModel>(CreateRows(Settings));
    }

    /// <summary>Gets the live settings instance being updated on Apply.</summary>
    public GeneratorSettings OriginalSettings { get; }

    /// <summary>Gets the independent settings copy.</summary>
    public GeneratorSettings Settings { get; }

    /// <summary>Gets reflected editable properties.</summary>
    public ObservableCollection<GeometryDashboardGeneratorSettingRowViewModel> Rows { get; }

    /// <summary>Gets the OR-combined input predicate collection.</summary>
    public SelectionPredicateCollection InputPredicates => Settings.InputPredicate;

    /// <summary>Gets or sets the selected input predicate.</summary>
    [ObservableProperty]
    public partial SelectionPredicate? SelectedPredicate { get; set; }

    /// <summary>Gets the extended predicate selection used by duplicate/remove actions.</summary>
    public ObservableCollection<SelectionPredicate> SelectedPredicates { get; } = [];

    /// <summary>Receives the window close callback.</summary>
    public Action<bool>? Close { get; set; }

    /// <summary>Replaces the selected input predicates from the list control.</summary>
    /// <param name="predicates">The selected predicates.</param>
    public void SetSelectedPredicates(IEnumerable<SelectionPredicate> predicates)
    {
        SelectedPredicates.Clear();
        foreach (var predicate in predicates) SelectedPredicates.Add(predicate);
        SelectedPredicate = SelectedPredicates.LastOrDefault();
    }

    /// <summary>Copies accepted values to the live generator.</summary>
    [RelayCommand]
    private void Apply()
    {
        Settings.CopyTo(OriginalSettings);
        Close?.Invoke(true);
    }

    /// <summary>Discards the independent settings copy.</summary>
    [RelayCommand]
    private void Cancel()
    {
        Close?.Invoke(false);
    }

    /// <summary>Adds an empty predicate.</summary>
    [RelayCommand]
    private void AddPredicate()
    {
        InputPredicates.Predicates.Add(new SelectionPredicate());
    }

    /// <summary>Duplicates the selected predicate.</summary>
    [RelayCommand]
    private void DuplicatePredicate()
    {
        var predicates = SelectedPredicates.Count > 0
            ? SelectedPredicates.ToArray()
            : SelectedPredicate is not null
                ? [SelectedPredicate]
                : [];
        foreach (var predicate in predicates)
        {
            int index = InputPredicates.Predicates.IndexOf(predicate);
            InputPredicates.Predicates.Insert(index + 1, (SelectionPredicate)predicate.Clone());
        }
    }

    /// <summary>Removes the selected predicate.</summary>
    [RelayCommand]
    private void RemovePredicate()
    {
        var predicates = SelectedPredicates.Count > 0
            ? SelectedPredicates.ToArray()
            : SelectedPredicate is not null
                ? [SelectedPredicate]
                : [];
        if (predicates.Length == 0 && InputPredicates.Predicates.Count > 0)
            predicates = [InputPredicates.Predicates[^1]];
        foreach (var predicate in predicates) InputPredicates.Predicates.Remove(predicate);
        SelectedPredicates.Clear();
        SelectedPredicate = null;
    }

    private static IEnumerable<GeometryDashboardGeneratorSettingRowViewModel> CreateRows(GeneratorSettings settings)
    {
        foreach (var property in settings.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead
                || !property.CanWrite
                || property.Name == nameof(GeneratorSettings.Generator)
                || property.PropertyType != typeof(bool) && property.PropertyType != typeof(double) && property.PropertyType != typeof(string))
                continue;

            yield return new GeometryDashboardGeneratorSettingRowViewModel(settings, property);
        }
    }
}

/// <summary>Provides a reflected generator property to Avalonia bindings.</summary>
public sealed class GeometryDashboardGeneratorSettingRowViewModel : ObservableObject
{
    private readonly PropertyInfo property;
    private readonly GeneratorSettings settings;
    private string? pendingValueText;

    /// <summary>Creates one reflected property row.</summary>
    public GeometryDashboardGeneratorSettingRowViewModel(GeneratorSettings settings, PropertyInfo property)
    {
        this.settings = settings;
        this.property = property;
    }

    /// <summary>Gets the property display name.</summary>
    public string Name => property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name;

    /// <summary>Gets the explanatory tooltip declared by the Core setting.</summary>
    public string? Description => property.GetCustomAttribute<DescriptionAttribute>()?.Description;

    /// <summary>Gets the underlying property value.</summary>
    public object? Value
    {
        get => property.GetValue(settings);
        set
        {
            property.SetValue(settings, value);
            pendingValueText = null;
            ValueTextError = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ValueText));
            OnPropertyChanged(nameof(ValueTextError));
        }
    }

    /// <summary>Gets or parses the reflected value using invariant text.</summary>
    public string ValueText
    {
        get => pendingValueText ?? Convert.ToString(Value, CultureInfo.InvariantCulture) ?? string.Empty;
        set
        {
            try
            {
                object? converted = Convert.ChangeType(
                    value,
                    Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType,
                    CultureInfo.InvariantCulture);
                Value = converted;
            }
            catch (FormatException)
            {
                pendingValueText = value;
                ValueTextError = "Number format error.";
                OnPropertyChanged(nameof(ValueTextError));
            }
            catch (OverflowException)
            {
                pendingValueText = value;
                ValueTextError = "Number format error.";
                OnPropertyChanged(nameof(ValueTextError));
            }
            catch (InvalidCastException)
            {
                pendingValueText = value;
                ValueTextError = "Number format error.";
                OnPropertyChanged(nameof(ValueTextError));
            }
        }
    }

    /// <summary>Gets the validation message for an invalid typed setting value.</summary>
    public string? ValueTextError { get; private set; }

    /// <summary>Gets whether the reflected value has a simple text editor.</summary>
    public bool IsTextEditable => property.PropertyType != typeof(bool);

    /// <summary>Gets whether this row represents a Boolean setting.</summary>
    public bool IsBoolean => property.PropertyType == typeof(bool);

    /// <summary>Gets or sets the Boolean setting value.</summary>
    public bool BooleanValue
    {
        get => (bool)(Value ?? false);
        set => Value = value;
    }

    /// <summary>Gets the reflected property type.</summary>
    public Type ValueType => property.PropertyType;
}

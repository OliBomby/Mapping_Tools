using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

/// <summary>Reflects typed generator settings into a compact dialog row model.</summary>
public sealed partial class GeometryDashboardGeneratorSettingsDialogViewModel : ObservableObject
{
    /// <summary>Creates the dialog over an independent generator-settings clone.</summary>
    public GeometryDashboardGeneratorSettingsDialogViewModel(GeneratorSettings settings)
    {
        OriginalSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        Settings = (GeneratorSettings)settings.Clone();

        SharedRows = new ObservableCollection<GeometryDashboardGeneratorSettingRowViewModel>(
            CreateRows(Settings, GetSharedProperties(Settings)));
        SpecificRows = new ObservableCollection<GeometryDashboardGeneratorSettingRowViewModel>(
            CreateRows(Settings, GetSpecificProperties(Settings)));
        Rows = new ObservableCollection<GeometryDashboardGeneratorSettingRowViewModel>(
            SharedRows.Concat(SpecificRows));

        SharedPredicateGroups = new ObservableCollection<GeometryDashboardPredicateCollectionViewModel>(
            CreatePredicateGroups(Settings, GetSharedProperties(Settings)));
        SpecificPredicateGroups = new ObservableCollection<GeometryDashboardPredicateCollectionViewModel>(
            CreatePredicateGroups(Settings, GetSpecificProperties(Settings)));
        PredicateGroups = new ObservableCollection<GeometryDashboardPredicateCollectionViewModel>(
            SharedPredicateGroups.Concat(SpecificPredicateGroups));
        InputPredicateRows = SharedPredicateGroups.First(group => ReferenceEquals(group.Model, Settings.InputPredicate)).Predicates;
    }

    /// <summary>Gets the live settings instance being updated on Apply.</summary>
    public GeneratorSettings OriginalSettings { get; }

    /// <summary>Gets the independent settings copy.</summary>
    public GeneratorSettings Settings { get; }

    /// <summary>Gets reflected editable properties.</summary>
    public ObservableCollection<GeometryDashboardGeneratorSettingRowViewModel> Rows { get; }

    /// <summary>Gets the shared generator settings shown in the first legacy card.</summary>
    public ObservableCollection<GeometryDashboardGeneratorSettingRowViewModel> SharedRows { get; }

    /// <summary>Gets generator-specific settings shown in the second legacy card.</summary>
    public ObservableCollection<GeometryDashboardGeneratorSettingRowViewModel> SpecificRows { get; }

    /// <summary>Gets all editable selection-predicate collections exposed by the generator settings.</summary>
    public ObservableCollection<GeometryDashboardPredicateCollectionViewModel> PredicateGroups { get; }

    /// <summary>Gets the shared selection-predicate collections shown in the first legacy card.</summary>
    public ObservableCollection<GeometryDashboardPredicateCollectionViewModel> SharedPredicateGroups { get; }

    /// <summary>Gets generator-specific selection-predicate collections shown in the second legacy card.</summary>
    public ObservableCollection<GeometryDashboardPredicateCollectionViewModel> SpecificPredicateGroups { get; }

    /// <summary>Gets whether a second card is needed for generator-specific settings.</summary>
    public bool HasSpecificSettings => SpecificRows.Count > 0 || SpecificPredicateGroups.Count > 0;

    /// <summary>Gets the OR-combined input predicate collection.</summary>
    public SelectionPredicateCollection InputPredicates => Settings.InputPredicate;

    /// <summary>Gets the observable input-predicate rows displayed by the Avalonia list.</summary>
    public ObservableCollection<SelectionPredicate> InputPredicateRows { get; }

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
        var predicate = new SelectionPredicate();
        InputPredicates.Predicates.Add(predicate);
        InputPredicateRows.Add(predicate);
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
        foreach (var predicate in predicates.OrderBy(predicate => IndexOfReference(InputPredicateRows, predicate)))
        {
            int index = IndexOfReference(InputPredicateRows, predicate);
            if (index < 0) continue;

            var copy = (SelectionPredicate)predicate.Clone();
            InputPredicates.Predicates.Insert(index + 1, copy);
            InputPredicateRows.Insert(index + 1, copy);
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
        foreach (var predicate in predicates)
        {
            int index = IndexOfReference(InputPredicateRows, predicate);
            if (index < 0) continue;

            InputPredicates.Predicates.RemoveAt(index);
            InputPredicateRows.RemoveAt(index);
        }
        SelectedPredicates.Clear();
        SelectedPredicate = null;
    }

    private static IEnumerable<PropertyInfo> GetSharedProperties(GeneratorSettings settings)
    {
        Type settingsType = settings.GetType();
        Type sharedType = settingsType.BaseType == typeof(GeneratorSettings)
            ? typeof(GeneratorSettings)
            : settingsType;

        return sharedType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
    }

    private static IEnumerable<PropertyInfo> GetSpecificProperties(GeneratorSettings settings)
    {
        Type settingsType = settings.GetType();
        if (settingsType.BaseType != typeof(GeneratorSettings))
            return [];

        return settingsType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
    }

    private static IEnumerable<GeometryDashboardGeneratorSettingRowViewModel> CreateRows(
        GeneratorSettings settings,
        IEnumerable<PropertyInfo> properties)
    {
        foreach (var property in properties)
        {
            if (!property.CanRead
                || !property.CanWrite
                || property.Name == nameof(GeneratorSettings.Generator)
                || property.PropertyType != typeof(bool) && property.PropertyType != typeof(double) && property.PropertyType != typeof(string))
                continue;

            yield return new GeometryDashboardGeneratorSettingRowViewModel(settings, property);
        }
    }

    private static IEnumerable<GeometryDashboardPredicateCollectionViewModel> CreatePredicateGroups(
        GeneratorSettings settings,
        IEnumerable<PropertyInfo> properties)
    {
        foreach (var property in properties.Where(property => property.CanRead
                                                               && property.CanWrite
                                                               && property.PropertyType == typeof(SelectionPredicateCollection)))
        {
            if (property.GetValue(settings) is SelectionPredicateCollection collection)
            {
                string name = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name;
                yield return new GeometryDashboardPredicateCollectionViewModel(name, collection);
            }
        }
    }

    private static int IndexOfReference(IList<SelectionPredicate> predicates, SelectionPredicate predicate)
    {
        for (int index = 0; index < predicates.Count; index++)
        {
            if (ReferenceEquals(predicates[index], predicate)) return index;
        }

        return -1;
    }
}

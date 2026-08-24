using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Desktop.Interactions.GeometryDashboard;

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


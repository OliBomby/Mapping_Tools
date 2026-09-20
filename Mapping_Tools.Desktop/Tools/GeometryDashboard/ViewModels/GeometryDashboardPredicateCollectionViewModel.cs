using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

/// <summary>Edits one ordered selection-predicate collection in the generator settings dialog.</summary>
public sealed partial class GeometryDashboardPredicateCollectionViewModel : ObservableObject
{
    /// <summary>Creates an editor for a predicate collection.</summary>
    /// <param name="name">The display name for the collection.</param>
    /// <param name="model">The settings collection edited by this view model.</param>
    public GeometryDashboardPredicateCollectionViewModel(string name, SelectionPredicateCollection model)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Predicates = new ObservableCollection<SelectionPredicate>(Model.Predicates);
    }

    /// <summary>Gets the display name for this predicate collection.</summary>
    public string Name { get; }

    /// <summary>Gets the settings collection kept in sync with the visible rows.</summary>
    public SelectionPredicateCollection Model { get; }

    /// <summary>Gets the observable predicate rows displayed by the Avalonia list.</summary>
    public ObservableCollection<SelectionPredicate> Predicates { get; }

    /// <summary>Gets the predicate rows currently selected in the list.</summary>
    public ObservableCollection<SelectionPredicate> SelectedPredicates { get; } = [];

    /// <summary>Gets or sets the most recently selected predicate.</summary>
    [ObservableProperty]
    public partial SelectionPredicate? SelectedPredicate { get; set; }

    /// <summary>Replaces the extended selection supplied by the Avalonia list.</summary>
    /// <param name="predicates">The selected predicate rows.</param>
    public void SetSelectedPredicates(IEnumerable<SelectionPredicate> predicates)
    {
        SelectedPredicates.Clear();
        foreach (var predicate in predicates) SelectedPredicates.Add(predicate);
        SelectedPredicate = SelectedPredicates.LastOrDefault();
    }

    /// <summary>Adds an empty predicate row.</summary>
    [RelayCommand]
    private void Add()
    {
        var predicate = new SelectionPredicate();
        Model.Predicates.Add(predicate);
        Predicates.Add(predicate);
    }

    /// <summary>Duplicates the selected predicate rows.</summary>
    [RelayCommand]
    private void Duplicate()
    {
        var predicates = SelectedPredicates.Count > 0
            ? SelectedPredicates.ToArray()
            : SelectedPredicate is not null
                ? [SelectedPredicate]
                : [];

        foreach (var predicate in predicates.OrderBy(IndexOfReference))
        {
            int index = IndexOfReference(predicate);
            if (index < 0) continue;

            var copy = (SelectionPredicate)predicate.Clone();
            Model.Predicates.Insert(index + 1, copy);
            Predicates.Insert(index + 1, copy);
        }
    }

    /// <summary>Removes the selected predicate rows or the last row when none is selected.</summary>
    [RelayCommand]
    private void Remove()
    {
        var predicates = SelectedPredicates.Count > 0
            ? SelectedPredicates.ToArray()
            : SelectedPredicate is not null
                ? [SelectedPredicate]
                : [];
        if (predicates.Length == 0 && Model.Predicates.Count > 0)
            predicates = [Model.Predicates[^1]];

        foreach (var predicate in predicates)
        {
            int index = IndexOfReference(predicate);
            if (index < 0) continue;

            Model.Predicates.RemoveAt(index);
            Predicates.RemoveAt(index);
        }

        SelectedPredicates.Clear();
        SelectedPredicate = null;
    }

    /// <summary>Removes only the predicate rows selected in the list.</summary>
    [RelayCommand]
    private void RemoveSelected()
    {
        foreach (var predicate in SelectedPredicates.ToArray())
        {
            int index = IndexOfReference(predicate);
            if (index < 0) continue;

            Model.Predicates.RemoveAt(index);
            Predicates.RemoveAt(index);
        }

        SelectedPredicates.Clear();
        SelectedPredicate = null;
    }

    private int IndexOfReference(SelectionPredicate predicate)
    {
        for (int index = 0; index < Predicates.Count; index++)
            if (ReferenceEquals(Predicates[index], predicate))
                return index;

        return -1;
    }
}

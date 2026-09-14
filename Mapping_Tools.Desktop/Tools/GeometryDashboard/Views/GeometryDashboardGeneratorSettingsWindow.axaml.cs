using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;

/// <summary>Hosts one generator's reflected settings.</summary>
public sealed partial class GeometryDashboardGeneratorSettingsWindow : Window
{
    /// <summary>Creates the generator settings window.</summary>
    public GeometryDashboardGeneratorSettingsWindow()
    {
        InitializeComponent();
    }

    private void PredicatesSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (eventArgs.Source != sender
            || sender is not ListBox listBox
            || listBox.DataContext is not GeometryDashboardPredicateCollectionViewModel collectionViewModel)
            return;

        collectionViewModel.SetSelectedPredicates(listBox.SelectedItems?.OfType<SelectionPredicate>() ?? []);
    }

}

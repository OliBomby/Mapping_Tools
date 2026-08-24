using System.Collections.ObjectModel;

namespace Mapping_Tools.Desktop.ViewModels.GeometryDashboard;

/// <summary>Contains a filtered generator group.</summary>
public sealed class GeometryDashboardGeneratorGroupViewModel
{
    /// <summary>Creates a group with the retained legacy heading.</summary>
    public GeometryDashboardGeneratorGroupViewModel(string name, IEnumerable<GeometryDashboardGeneratorViewModel> generators)
    {
        Name = name;
        Generators = new ObservableCollection<GeometryDashboardGeneratorViewModel>(generators);
    }

    /// <summary>Gets the group heading.</summary>
    public string Name { get; }

    /// <summary>Gets the rows in this group.</summary>
    public ObservableCollection<GeometryDashboardGeneratorViewModel> Generators { get; }

    /// <summary>Gets the visible row count rendered in the heading.</summary>
    public int ItemCount => Generators.Count;
}


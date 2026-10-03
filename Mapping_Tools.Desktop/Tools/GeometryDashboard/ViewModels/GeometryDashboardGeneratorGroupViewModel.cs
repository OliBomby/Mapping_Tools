using System.Collections.ObjectModel;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Desktop.Localization;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

/// <summary>Contains a filtered generator group.</summary>
public sealed class GeometryDashboardGeneratorGroupViewModel : LocalizedObservableObject
{
    private readonly GeneratorType type;

    /// <summary>Creates a group with the retained legacy heading.</summary>
    public GeometryDashboardGeneratorGroupViewModel(GeneratorType type, IEnumerable<GeometryDashboardGeneratorViewModel> generators)
    {
        this.type = type;
        Generators = new ObservableCollection<GeometryDashboardGeneratorViewModel>(generators);
    }

    /// <summary>Gets the group heading.</summary>
    public string Name => type switch
    {
        GeneratorType.Basic => DesktopStrings.GeometryDashboard_GroupBasic,
        GeneratorType.Intermediate => DesktopStrings.GeometryDashboard_GroupIntermediate,
        GeneratorType.Advanced => DesktopStrings.GeometryDashboard_GroupAdvanced,
        _ => type.ToString(),
    };

    /// <summary>Gets the rows in this group.</summary>
    public ObservableCollection<GeometryDashboardGeneratorViewModel> Generators { get; }

    /// <summary>Gets the visible row count rendered in the heading.</summary>
    public int ItemCount => Generators.Count;

    /// <summary>Gets the localized noun suffix following the separately styled visible row count.</summary>
    public string ItemCountLabel => ItemCount == 1
        ? DesktopStrings.GeometryDashboard_OneItem
        : DesktopStrings.GeometryDashboard_ManyItems;
}

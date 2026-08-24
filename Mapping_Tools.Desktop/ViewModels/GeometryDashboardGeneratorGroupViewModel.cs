using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Desktop.Shell;
using Material.Icons;

namespace Mapping_Tools.Desktop.ViewModels;

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


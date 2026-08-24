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
    public string Tooltip => Model.Tooltip;

    /// <summary>Gets the settings object shown in the row.</summary>
    public GeneratorSettings Settings => Model.Settings;

    /// <summary>Shows this generator's settings dialog.</summary>
    [RelayCommand]
    private Task OpenSettingsAsync()
    {
        return owner.ShowGeneratorSettingsAsync(this);
    }
}


using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.TumourGenerating;

namespace Mapping_Tools.Application.Tools.TumourGenerator;

/// <summary>Stores the persisted Tumour Generator 2 run settings.</summary>
public sealed class TumourGeneratorProject : TumourGeneratorOptions
{
    /// <summary>Creates a project with the selected import mode and one default layer.</summary>
    public TumourGeneratorProject()
    {
        ImportModeSetting = TumourImportMode.Selected;
        TumourLayers.Add(TumourLayer.GetDefaultLayer());
    }

    /// <summary>Gets or sets the object-selection source used by import and run.</summary>
    public TumourImportMode ImportModeSetting { get; set; }

    /// <summary>Gets or sets the time-code expression used in time mode.</summary>
    public string TimeCode { get; set; } = string.Empty;
}


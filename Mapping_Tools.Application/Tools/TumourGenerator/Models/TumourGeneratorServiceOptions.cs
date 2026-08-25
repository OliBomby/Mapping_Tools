using Mapping_Tools.Core.Tools.TumourGenerating.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Application.Tools.TumourGenerator.Models;

/// <summary>Stores the persisted Tumour Generator 2 run settings.</summary>
public class TumourGeneratorServiceOptions : TumourGeneratorEngineOptions
{
    /// <summary>Creates a project with the selected import mode and one default layer.</summary>
    public TumourGeneratorServiceOptions()
    {
        ImportModeSetting = HitObjectSelectionMode.Selected;
        TumourLayers.Add(TumourLayer.GetDefaultLayer());
    }

    /// <summary>Gets or sets the object-selection source used by import and run.</summary>
    public HitObjectSelectionMode ImportModeSetting { get; set; }

    /// <summary>Gets or sets the time-code expression used in time mode.</summary>
    public string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets whether slider velocity is corrected after generation.</summary>
    public bool FixSv { get; set; } = true;

    /// <summary>Gets or sets whether corrected velocity is delegated to BPM redlines.</summary>
    public bool DelegateToBpm { get; set; }

    /// <summary>Gets or sets whether delegated velocity removes slider ticks.</summary>
    public bool RemoveSliderTicks { get; set; }

}

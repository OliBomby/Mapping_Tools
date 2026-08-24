using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.TumourGenerating.Models;

/// <summary>Groups the framework-neutral settings used by the tumour generator.</summary>
public class TumourGeneratorOptions
{
    /// <summary>Gets or sets the configured ordered tumour layers.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<TumourLayer> TumourLayers { get; set; } = [];

    /// <summary>Gets or sets whether only middle anchors are retained.</summary>
    public bool JustMiddleAnchors { get; set; }

    /// <summary>Gets or sets the global tumour size scalar.</summary>
    public double Scale { get; set; } = 1;

    /// <summary>Gets or sets whether intelligent path reconstruction is skipped.</summary>
    public bool DebugConstruction { get; set; }

    /// <summary>Gets or sets whether slider velocity is corrected after generation.</summary>
    public bool FixSv { get; set; } = true;

    /// <summary>Gets or sets whether corrected velocity is delegated to BPM redlines.</summary>
    public bool DelegateToBpm { get; set; }

    /// <summary>Gets or sets whether delegated velocity removes slider ticks.</summary>
    public bool RemoveSliderTicks { get; set; }
}


using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Desktop.Tools.TumourGenerator.Models;

/// <summary>Stores the persisted Tumour Generator project.</summary>
public sealed class TumourGeneratorProject : TumourGeneratorServiceOptions
{
    /// <summary>Gets or sets the slider shown in the Tumour Generator preview.</summary>
    public HitObject PreviewHitObject { get; set; } = new("0,0,0,2,0,L|256:0,1,256");

    /// <summary>Gets or sets whether advanced layer controls are visible.</summary>
    public bool AdvancedOptions { get; set; }
}

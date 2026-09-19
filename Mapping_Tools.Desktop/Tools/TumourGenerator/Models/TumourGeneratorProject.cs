using Mapping_Tools.Application.Tools.TumourGenerator.Models;

namespace Mapping_Tools.Desktop.Tools.TumourGenerator.Models;

/// <summary>Stores the persisted Tumour Generator project.</summary>
public sealed class TumourGeneratorProject : TumourGeneratorServiceOptions
{
    /// <summary>Gets or sets whether advanced layer controls are visible.</summary>
    public bool AdvancedOptions { get; set; }
}

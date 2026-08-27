using Mapping_Tools.Core.Tools.HitsoundCopier.Models;

namespace Mapping_Tools.Application.Tools.HitsoundCopier;

/// <summary>Represents the complete Hitsound Copier state persisted by the shell.</summary>
public class HitsoundCopierServiceOptions : HitsoundCopierEngineOptions
{
    /// <summary>Gets or sets the optional source beatmap path.</summary>
    public string PathFrom { get; set; } = string.Empty;

    /// <summary>Gets or sets vertical-bar-separated target beatmap paths.</summary>
    public string PathTo { get; set; } = string.Empty;

}

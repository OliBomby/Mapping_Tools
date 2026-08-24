using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.Sliderator;

/// <summary>Chooses the source objects imported into Sliderator.</summary>
public enum SlideratorImportMode
{
    /// <summary>Uses the objects selected in the live editor.</summary>
    Selected,

    /// <summary>Uses objects covered by editor bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects matched by the time-code expression.</summary>
    Time,
}


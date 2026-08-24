namespace Mapping_Tools.Application.Tools.TumourGenerator.Models;

/// <summary>Chooses which objects are imported or transformed by Tumour Generator 2.</summary>
public enum TumourImportMode
{
    /// <summary>Uses the objects selected in the live osu! editor.</summary>
    Selected,

    /// <summary>Uses objects covered by saved bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects matched by the time-code expression.</summary>
    Time,

    /// <summary>Uses every object in the beatmap.</summary>
    Everything,
}


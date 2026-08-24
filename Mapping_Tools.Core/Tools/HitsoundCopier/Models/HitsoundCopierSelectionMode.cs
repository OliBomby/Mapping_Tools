namespace Mapping_Tools.Core.Tools.HitsoundCopier.Models;

/// <summary>Identifies the source objects used by Hitsound Copier.</summary>
public enum HitsoundCopierSelectionMode
{
    /// <summary>Uses objects selected in the live osu! editor.</summary>
    Selected,

    /// <summary>Uses objects covered by beatmap bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects returned by the legacy osu! time-code query.</summary>
    Time,

    /// <summary>Uses every object in the source beatmap.</summary>
    Everything,
}


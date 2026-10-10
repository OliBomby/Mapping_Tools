namespace Mapping_Tools.Core.BeatmapHelper.Enums;

/// <summary>Defines the source of beatmap hit objects processed by a tool.</summary>
public enum HitObjectSelectionMode
{
    /// <summary>Uses the objects selected in the live editor.</summary>
    Selected = 0,

    /// <summary>Uses objects covered by editor bookmarks.</summary>
    Bookmarked = 1,

    /// <summary>Uses objects matched by a legacy osu! time-code expression.</summary>
    Time = 2,

    /// <summary>Uses every hit object in the beatmap.</summary>
    Everything = 3,
}

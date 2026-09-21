namespace Mapping_Tools.Application.Settings.Models;

/// <summary>Chooses how Mapping Tools locates osu!'s current beatmap.</summary>
public enum CurrentBeatmapFetchingMode
{
    /// <summary>Do not query osu! for a current beatmap.</summary>
    Disabled,

    /// <summary>Read the current beatmap path from osu!'s process memory.</summary>
    MemoryRead,

    /// <summary>Read the current beatmap path through MTIPC.</summary>
    Mtipc,
}

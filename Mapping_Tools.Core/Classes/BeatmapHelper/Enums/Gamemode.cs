namespace Mapping_Tools.Core.Classes.BeatmapHelper.Enums
{
    /// <summary>
    /// Identifies the osu! ruleset encoded by the beatmap's <c>Mode</c> field.
    /// </summary>
#nullable disable

    public enum GameMode
    {
        /// <summary>
        /// osu!standard, using circles, sliders, and spinners.
        /// </summary>
        Standard = 0,
        /// <summary>
        /// osu!taiko drum gameplay.
        /// </summary>
        Taiko = 1,
        /// <summary>
        /// osu!catch fruit-catching gameplay.
        /// </summary>
        Catch = 2,
        /// <summary>
        /// osu!mania key-column gameplay.
        /// </summary>
        Mania = 3
    }
}

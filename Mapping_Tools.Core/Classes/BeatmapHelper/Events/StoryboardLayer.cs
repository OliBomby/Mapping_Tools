namespace Mapping_Tools.Core.Classes.BeatmapHelper.Events
{
    /// <summary>
    /// The layers according to the osu! Storyboard.
    /// </summary>
#nullable disable

    public enum StoryboardLayer
    {
        /// <summary>
        /// Behind all gameplay and storyboard content.
        /// </summary>
        Background = 0,
        /// <summary>
        /// Shown on the fail screen.
        /// </summary>
        Fail = 1,
        /// <summary>
        /// Shown on the pass screen.
        /// </summary>
        Pass = 2,
        /// <summary>
        /// In front of the background layer during gameplay.
        /// </summary>
        Foreground = 3,
        /// <summary>
        /// Above foreground storyboard content.
        /// </summary>
        Overlay = 4,
        /// <summary>
        /// Difficulty-specific background layer.
        /// </summary>
        DifficultyBackground = 5,
        /// <summary>
        /// Difficulty-specific fail layer.
        /// </summary>
        DifficultyFail = 6,
        /// <summary>
        /// Difficulty-specific foreground layer.
        /// </summary>
        DifficultyForeground = 7,
        /// <summary>
        /// Difficulty-specific overlay layer.
        /// </summary>
        DifficultyOverlay = 8
    }
}

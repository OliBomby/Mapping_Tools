namespace Mapping_Tools.Core.Classes.BeatmapHelper.Events {
#nullable disable

    /// <summary>
    /// Identifies the point of a storyboard texture placed at its command position.
    /// </summary>
    public enum Origin {
        /// <summary>
        /// The texture's top-left corner.
        /// </summary>
        TopLeft,
        /// <summary>
        /// The texture's centre.
        /// </summary>
        Centre,
        /// <summary>
        /// The midpoint of the texture's left edge.
        /// </summary>
        CentreLeft,
        /// <summary>
        /// The texture's top-right corner.
        /// </summary>
        TopRight,
        /// <summary>
        /// The midpoint of the texture's bottom edge.
        /// </summary>
        BottomCentre,
        /// <summary>
        /// The midpoint of the texture's top edge.
        /// </summary>
        TopCentre,
        /// <summary>
        /// A nonstandard origin token preserved from input.
        /// </summary>
        Custom,
        /// <summary>
        /// The midpoint of the texture's right edge.
        /// </summary>
        CentreRight,
        /// <summary>
        /// The texture's bottom-left corner.
        /// </summary>
        BottomLeft,
        /// <summary>
        /// The texture's bottom-right corner.
        /// </summary>
        BottomRight
    }
}

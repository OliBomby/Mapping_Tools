namespace Mapping_Tools.Classes.ExternalFileUtil.Reaper
{
    /// <summary>
    /// The shape of the current envelope point.
    /// </summary>
    public enum EnvelopeShape
    {
        /// <summary>
        /// The linear envelope shape with no transitioning to the next point.
        /// </summary>
        Square,

        /// <summary>
        /// A gradual transition to the next point.
        /// </summary>
        Transitional,
    }
}
namespace Mapping_Tools.Classes.BeatmapHelper.Enums {
    /// <summary>
    /// The types of samples used for inherited timing points and hitobjects themselves.
    /// </summary>
    public enum SampleSet {
        /// <summary>
        /// (Hitobject only) Uses the current inherited timing points' hitsound sampleset and custom list.
        /// </summary>
        Auto = 0,
        /// <summary>
        /// The sampleset of Normal.
        /// </summary>
        Normal = 1,
        /// <summary>
        /// The sampleset of Soft.
        /// </summary>
        Soft = 2,
        /// <summary>
        /// The sampleset of Drum.
        /// </summary>
        Drum = 3
    }
}

namespace Mapping_Tools_Core.Tools.RhythmGuide {
    public enum SelectionMode {
        /// <summary>
        /// Makes a rhythm guide object for every timeline object.
        /// </summary>
        AllEvents,
        /// <summary>
        /// Makes a rhythm guide object for every timeline object that makes a hitsound.
        /// </summary>
        HitsoundEvents
    }
}
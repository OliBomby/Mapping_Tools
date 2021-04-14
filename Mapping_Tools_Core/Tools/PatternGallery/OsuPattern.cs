using System;

namespace Mapping_Tools_Core.Tools.PatternGallery {
    /// <summary>
    /// Must store the objects, the greenlines, the timing, the global SV, the tickrate, the difficulty settings,
    /// the hitsounds, absolute times and positions, combocolour index, combo numbers, stack leniency, gamemode.
    /// Also store additional metadata such as the name, the date it was saved, use count, the map title, artist, diffname, and mapper.
    /// </summary>
    public class OsuPattern : IOsuPattern {
        #region Fields

        public string Name { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastUsedTime { get; set; }
        public int UseCount { get; set; }
        public string Filename { get; set; }
        public int ObjectCount { get; set; }
        public TimeSpan Duration { get; set; }
        public double BeatLength { get; set; }

        #endregion
    }
}
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Classes.HitsoundStuff {
    /// <summary>
    /// Defines a positional hitsound-mapping rule in which -1 coordinates act as wildcards.
    /// </summary>
    public class HitsoundZone
    {
        /// <summary>
        /// Creates a wildcard zone using inherited sample settings.
        /// </summary>
        public HitsoundZone() { }

        /// <summary>
        /// Creates a complete zone for persistence or editing.
        /// </summary>
        /// <param name="name">The user-facing rule name.</param>
        /// <param name="filename">An optional explicit sample filename.</param>
        /// <param name="xPos">The target playfield X coordinate, or -1 to ignore X.</param>
        /// <param name="yPos">The target playfield Y coordinate, or -1 to ignore Y.</param>
        /// <param name="hitsound">The target hitsound layer.</param>
        /// <param name="sampleSet">The normal-layer sample family.</param>
        /// <param name="additionsSet">The addition-layer sample family.</param>
        /// <param name="customIndex">The custom sample index.</param>
        public HitsoundZone(string name, string filename, double xPos, double yPos, Hitsound hitsound, SampleSet sampleSet, SampleSet additionsSet, int customIndex) {
            Name = name;
            Filename = filename;
            XPos = xPos;
            YPos = yPos;
            Hitsound = hitsound;
            SampleSet = sampleSet;
            AdditionsSet = additionsSet;
            CustomIndex = customIndex;
        }

        /// <summary>
        /// Calculates Euclidean distance from a playfield point while ignoring wildcard axes.
        /// </summary>
        /// <param name="pos">The point to test.</param>
        /// <returns>Distance in playfield pixels over the constrained axes.</returns>
        public double Distance(Vector2 pos) {
            double dx = XPos == -1 ? 0 : XPos - pos.X;
            double dy = YPos == -1 ? 0 : YPos - pos.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Copies persisted settings into a new zone.
        /// </summary>
        /// <returns>An independently mutable zone.</returns>
        public HitsoundZone Copy() {
            return new HitsoundZone(Name, Filename, XPos, YPos, Hitsound, SampleSet, AdditionsSet, CustomIndex);
        }

        /// <summary>
        /// Gets or sets the explicit custom sample filename.
        /// </summary>
        public string Filename { get; set; } = "";

        /// <summary>
        /// Gets or sets the user-facing rule name.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the target X coordinate, or -1 to match any X.
        /// </summary>
        public double XPos { get; set; } = -1;

        /// <summary>
        /// Gets or sets the target Y coordinate, or -1 to match any Y.
        /// </summary>
        public double YPos { get; set; } = -1;

        /// <summary>
        /// Gets or sets the normal, whistle, finish, or clap layer matched by the zone.
        /// </summary>
        public Hitsound Hitsound { get; set; } = Hitsound.Normal;

        /// <summary>
        /// Gets or sets the normal-layer sample family.
        /// </summary>
        public SampleSet SampleSet { get; set; } = SampleSet.None;

        /// <summary>
        /// Gets or sets the whistle/finish/clap sample family.
        /// </summary>
        public SampleSet AdditionsSet { get; set; } = SampleSet.None;

        /// <summary>
        /// Gets or sets the custom sample index assigned by the zone.
        /// </summary>
        public int CustomIndex { get; set; }

    }
}

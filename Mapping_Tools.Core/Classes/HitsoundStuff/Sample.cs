using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.SystemTools;

namespace Mapping_Tools.Core.Classes.HitsoundStuff {
    /// <summary>
    /// Assigns a generated sound to one osu! sample-set and hitsound layer at a package time.
    /// </summary>
    public class Sample : BindableBase {
        private SampleGeneratingArgs sampleArgs;
        private int priority;
        private double outsideVolume;
        private SampleSet sampleSet;
        private Hitsound hitsound;

        /// <summary>
        /// Gets or sets the source and transformations used to produce the sound.
        /// </summary>
        public SampleGeneratingArgs SampleArgs {
            get => sampleArgs;
            set => Set(ref sampleArgs, value);
        }

        /// <summary>
        /// Gets or sets conflict priority; lower values win when deriving sample-set metadata.
        /// </summary>
        public int Priority {
            get => priority;
            set => Set(ref priority, value);
        }

        /// <summary>
        /// Gets or sets the event-level volume multiplier applied outside sample generation.
        /// </summary>
        public double OutsideVolume {
            get => outsideVolume;
            set => Set(ref outsideVolume, value);
        }

        /// <summary>
        /// Gets or sets the osu! normal, soft, or drum family receiving the sample.
        /// </summary>
        public SampleSet SampleSet {
            get => sampleSet;
            set => Set(ref sampleSet, value);
        }

        /// <summary>
        /// Gets or sets the normal, whistle, finish, or clap layer receiving the sample.
        /// </summary>
        public Hitsound Hitsound {
            get => hitsound;
            set => Set(ref hitsound, value);
        }

        /// <summary>
        /// Indicates that this sample supplies the normal layer.
        /// </summary>
        public bool Normal => Hitsound == Hitsound.Normal;
        /// <summary>
        /// Indicates that this sample supplies the whistle layer.
        /// </summary>
        public bool Whistle => Hitsound == Hitsound.Whistle;
        /// <summary>
        /// Indicates that this sample supplies the finish layer.
        /// </summary>
        public bool Finish => Hitsound == Hitsound.Finish;
        /// <summary>
        /// Indicates that this sample supplies the clap layer.
        /// </summary>
        public bool Clap => Hitsound == Hitsound.Clap;

        /// <summary>
        /// Creates a unity-volume, highest-priority normal sample in the normal set.
        /// </summary>
        public Sample() {
            sampleArgs = new SampleGeneratingArgs();
            outsideVolume = 1;
            priority = 0;
            sampleSet = SampleSet.Normal;
            hitsound = Hitsound.Normal;
        }

        /// <summary>
        /// Creates a fully specified package sample.
        /// </summary>
        /// <param name="sampleSet">The target osu! sample family.</param>
        /// <param name="hitsound">The target sample layer.</param>
        /// <param name="sampleArgs">The source and transformations.</param>
        /// <param name="priority">Conflict priority; lower values take precedence.</param>
        /// <param name="outsideVolume">The event-level volume multiplier.</param>
        public Sample(SampleSet sampleSet, Hitsound hitsound, SampleGeneratingArgs sampleArgs, int priority, double outsideVolume) {
            this.sampleArgs = sampleArgs;
            this.outsideVolume = outsideVolume;
            this.priority = priority;
            this.sampleSet = sampleSet;
            this.hitsound = hitsound;
        }

        /// <summary>
        /// Converts an import layer into a package sample while copying its generation arguments.
        /// </summary>
        /// <param name="hl">The imported hitsound layer.</param>
        public Sample(HitsoundLayer hl) {
            sampleArgs = hl.SampleArgs.Copy();  // Copy so any changes made to these sample args do not carry over to the layers
            outsideVolume = 1;
            priority = hl.Priority;
            sampleSet = hl.SampleSet;
            hitsound = hl.Hitsound;
        }

        /// <summary>
        /// Copies the sample and its nested generation arguments.
        /// </summary>
        /// <returns>An independently mutable sample.</returns>
        public Sample Copy() {
            return new Sample(SampleSet, Hitsound, SampleArgs.Copy(), Priority, OutsideVolume);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() {
            return $"{SampleArgs}, outside volume: {OutsideVolume}, priority: {Priority}, sampleset: {SampleSet}, hitsound: {Hitsound}";
        }
    }
}

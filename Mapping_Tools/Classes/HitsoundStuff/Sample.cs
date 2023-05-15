using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// 
    /// </summary>
    public class Sample : BindableBase {
        private SampleGeneratingArgs sampleArgs;
        private int priority;
        private double outsideVolume;
        private SampleSet sampleSet;
        private Hitsound hitsound;

        /// <summary>
        /// 
        /// </summary>
        public SampleGeneratingArgs SampleArgs {
            get => sampleArgs;
            set => Set(ref sampleArgs, value);
        }

        public int Priority {
            get => priority;
            set => Set(ref priority, value);
        }

        public double OutsideVolume {
            get => outsideVolume;
            set => Set(ref outsideVolume, value);
        }

        public SampleSet SampleSet {
            get => sampleSet;
            set => Set(ref sampleSet, value);
        }

        public Hitsound Hitsound {
            get => hitsound;
            set => Set(ref hitsound, value);
        }

        public bool Normal => Hitsound == Hitsound.Normal;
        public bool Whistle => Hitsound == Hitsound.Whistle;
        public bool Finish => Hitsound == Hitsound.Finish;
        public bool Clap => Hitsound == Hitsound.Clap;

        public Sample() {
            sampleArgs = new SampleGeneratingArgs();
            outsideVolume = 1;
            priority = 0;
            sampleSet = SampleSet.Normal;
            hitsound = Hitsound.Normal;
        }

        public Sample(SampleSet sampleSet, Hitsound hitsound, SampleGeneratingArgs sampleArgs, int priority, double outsideVolume) {
            this.sampleArgs = sampleArgs;
            this.outsideVolume = outsideVolume;
            this.priority = priority;
            this.sampleSet = sampleSet;
            this.hitsound = hitsound;
        }

        public Sample(HitsoundLayer hl) {
            sampleArgs = hl.SampleArgs.Copy();  // Copy so any changes made to these sample args do not carry over to the layers
            outsideVolume = 1;
            priority = hl.Priority;
            sampleSet = hl.SampleSet;
            hitsound = hl.Hitsound;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// 
    /// </summary>
    public class Sample : BindableBase {
        private SampleGeneratingArgs _sampleArgs;
        private int _priority;
        private double _outsideVolume;
        private SampleSet _sampleSet;
        private Hitsound _hitsound;

        /// <summary>
        /// 
        /// </summary>
        public SampleGeneratingArgs SampleArgs {
            get => _sampleArgs;
            set => Set(ref _sampleArgs, value);
        }

        public int Priority {
            get => _priority;
            set => Set(ref _priority, value);
        }

        public double OutsideVolume {
            get => _outsideVolume;
            set => Set(ref _outsideVolume, value);
        }

        public SampleSet SampleSet {
            get => _sampleSet;
            set => Set(ref _sampleSet, value);
        }

        public Hitsound Hitsound {
            get => _hitsound;
            set => Set(ref _hitsound, value);
        }

        public bool Normal => Hitsound == Hitsound.Normal;
        public bool Whistle => Hitsound == Hitsound.Whistle;
        public bool Finish => Hitsound == Hitsound.Finish;
        public bool Clap => Hitsound == Hitsound.Clap;

        public Sample() {
            _sampleArgs = new SampleGeneratingArgs();
            _outsideVolume = 1;
            _priority = 0;
            _sampleSet = SampleSet.Normal;
            _hitsound = Hitsound.Normal;
        }

        public Sample(SampleSet sampleSet, Hitsound hitsound, SampleGeneratingArgs sampleArgs, int priority, double outsideVolume) {
            _sampleArgs = sampleArgs;
            _outsideVolume = outsideVolume;
            _priority = priority;
            _sampleSet = sampleSet;
            _hitsound = hitsound;
        }

        public Sample(HitsoundLayer hl) {
            _sampleArgs = hl.SampleArgs.Copy();  // Copy so any changes made to these sample args do not carry over to the layers
            _outsideVolume = 1;
            _priority = hl.Priority;
            _sampleSet = hl.SampleSet;
            _hitsound = hl.Hitsound;
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

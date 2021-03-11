namespace Mapping_Tools.Classes.Tools.HitsoundStudio {
    /// <summary>
    /// 
    /// </summary>
    public class BindableSample : BindableBase {
        private UISampleGeneratingArgs _sampleArgs;
        private int _priority;
        private double _outsideVolume;
        private SampleSet _sampleSet;
        private Hitsound _hitsound;

        /// <summary>
        /// 
        /// </summary>
        public UISampleGeneratingArgs SampleGeneratingArgs {
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

        public BindableSample() {
            _sampleArgs = new UISampleGeneratingArgs();
            _outsideVolume = 1;
            _priority = 0;
            _sampleSet = SampleSet.Normal;
            _hitsound = Hitsound.Normal;
        }

        public BindableSample(SampleSet sampleSet, Hitsound hitsound, UISampleGeneratingArgs sampleArgs, int priority, double outsideVolume) {
            _sampleArgs = sampleArgs;
            _outsideVolume = outsideVolume;
            _priority = priority;
            _sampleSet = sampleSet;
            _hitsound = hitsound;
        }

        public BindableSample(HitsoundLayer hl) {
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
        public object Clone() {
            return new BindableSample(SampleSet, Hitsound, SampleGeneratingArgs.Copy(), Priority, OutsideVolume);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() {
            return $"{SampleGeneratingArgs}, outside volume: {OutsideVolume}, priority: {Priority}, sampleset: {SampleSet}, hitsound: {Hitsound}";
        }
    }
}

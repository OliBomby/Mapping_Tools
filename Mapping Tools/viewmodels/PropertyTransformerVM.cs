using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class PropertyTransformerVM : BindableBase{
        #region Properties

        private double _timingpointOffsetMultiplier;
        public double TimingpointOffsetMultiplier {
            get => _timingpointOffsetMultiplier;
            set => Set(ref _timingpointOffsetMultiplier, value);
        }

        private double timingpointOffsetOffset;
        public double TimingpointOffsetOffset {
            get => timingpointOffsetOffset;
            set => Set(ref timingpointOffsetOffset, value);
        }

        private double timingpointBPMMultiplier;
        public double TimingpointBPMMultiplier {
            get => timingpointBPMMultiplier;
            set => Set(ref timingpointBPMMultiplier, value);
        }

        private double timingpointBPMOffset;
        public double TimingpointBPMOffset {
            get => timingpointBPMOffset;
            set => Set(ref timingpointBPMOffset, value);
        }

        private double timingpointSVMultiplier;
        public double TimingpointSVMultiplier {
            get => timingpointSVMultiplier;
            set => Set(ref timingpointSVMultiplier, value);
        }

        private double timingpointSVOffset;
        public double TimingpointSVOffset {
            get => timingpointSVOffset;
            set => Set(ref timingpointSVOffset, value);
        }

        private double timingpointIndexMultiplier;
        public double TimingpointIndexMultiplier {
            get => timingpointIndexMultiplier;
            set => Set(ref timingpointIndexMultiplier, value);
        }

        private double timingpointIndexOffset;
        public double TimingpointIndexOffset {
            get => timingpointIndexOffset;
            set => Set(ref timingpointIndexOffset, value);
        }

        private double timingpointVolumeMultiplier;
        public double TimingpointVolumeMultiplier {
            get => timingpointVolumeMultiplier;
            set => Set(ref timingpointVolumeMultiplier, value);
        }

        private double timingpointVolumeOffset;
        public double TimingpointVolumeOffset {
            get => timingpointVolumeOffset;
            set => Set(ref timingpointVolumeOffset, value);
        }

        private double hitObjectTimeMultiplier;
        public double HitObjectTimeMultiplier {
            get => hitObjectTimeMultiplier;
            set => Set(ref hitObjectTimeMultiplier, value);
        }

        private double hitObjectTimeOffset;
        public double HitObjectTimeOffset {
            get => hitObjectTimeOffset;
            set => Set(ref hitObjectTimeOffset, value);
        }

        private double bookmarkTimeMultiplier;
        public double BookmarkTimeMultiplier {
            get => bookmarkTimeMultiplier;
            set => Set(ref bookmarkTimeMultiplier, value);
        }

        private double bookmarkTimeOffset;
        public double BookmarkTimeOffset {
            get => bookmarkTimeOffset;
            set => Set(ref bookmarkTimeOffset, value);
        }

        private double sbSampleTimeMultiplier;
        public double SBSampleTimeMultiplier {
            get => sbSampleTimeMultiplier;
            set => Set(ref sbSampleTimeMultiplier, value);
        }

        private double sbSampleTimeOffset;
        public double SBSampleTimeOffset {
            get => sbSampleTimeOffset;
            set => Set(ref sbSampleTimeOffset, value);
        }

        private bool clipProperties;
        public bool ClipProperties {
            get => clipProperties;
            set => Set(ref clipProperties, value);
        }

        private bool enableFilters;
        public bool EnableFilters {
            get => enableFilters;
            set => Set(ref enableFilters, value);
        }

        private double matchFilter;
        public double MatchFilter {
            get => matchFilter;
            set => Set(ref matchFilter, value);
        }

        private double minTimeFilter;
        public double MinTimeFilter {
            get => minTimeFilter;
            set => Set(ref minTimeFilter, value);
        }

        private double maxTimeFilter;
        public double MaxTimeFilter {
            get => maxTimeFilter;
            set => Set(ref maxTimeFilter, value);
        }

        #endregion

        [JsonIgnore]
        public string[] ExportPaths { get; set; }

        public PropertyTransformerVM() {
            TimingpointOffsetMultiplier = 1;
            TimingpointOffsetOffset = 0;
            TimingpointBPMMultiplier = 1;
            TimingpointBPMOffset = 0;
            TimingpointSVMultiplier = 1;
            TimingpointSVOffset = 0;
            TimingpointIndexMultiplier = 1;
            TimingpointIndexOffset = 0;
            TimingpointVolumeMultiplier = 1;
            TimingpointVolumeOffset = 0;
            HitObjectTimeMultiplier = 1;
            HitObjectTimeOffset = 0;
            BookmarkTimeMultiplier = 1;
            BookmarkTimeOffset = 0;
            SBSampleTimeMultiplier = 1;
            SBSampleTimeOffset = 0;

            ClipProperties = false;
            EnableFilters = false;
            MatchFilter = -1;
            MinTimeFilter = -1;
            MaxTimeFilter = -1;
        }
    }
}

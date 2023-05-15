using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class PropertyTransformerVm : BindableBase{
        #region multipliers and offsets

        private double timingpointOffsetMultiplier;
        public double TimingpointOffsetMultiplier {
            get => timingpointOffsetMultiplier;
            set {
                if (Set(ref timingpointOffsetMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double timingpointOffsetOffset;
        public double TimingpointOffsetOffset {
            get => timingpointOffsetOffset;
            set {
                if (Set(ref timingpointOffsetOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double timingpointBpmMultiplier;
        public double TimingpointBpmMultiplier {
            get => timingpointBpmMultiplier;
            set => Set(ref timingpointBpmMultiplier, value);
        }

        private double timingpointBpmOffset;
        public double TimingpointBpmOffset {
            get => timingpointBpmOffset;
            set => Set(ref timingpointBpmOffset, value);
        }

        private double timingpointSvMultiplier;
        public double TimingpointSvMultiplier {
            get => timingpointSvMultiplier;
            set => Set(ref timingpointSvMultiplier, value);
        }

        private double timingpointSvOffset;
        public double TimingpointSvOffset {
            get => timingpointSvOffset;
            set => Set(ref timingpointSvOffset, value);
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
            set {
                if (Set(ref hitObjectTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double hitObjectTimeOffset;
        public double HitObjectTimeOffset {
            get => hitObjectTimeOffset;
            set {
                if (Set(ref hitObjectTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double bookmarkTimeMultiplier;
        public double BookmarkTimeMultiplier {
            get => bookmarkTimeMultiplier;
            set {
                if (Set(ref bookmarkTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double bookmarkTimeOffset;
        public double BookmarkTimeOffset {
            get => bookmarkTimeOffset;
            set {
                if (Set(ref bookmarkTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double sbEventTimeMultiplier;
        public double SbEventTimeMultiplier {
            get => sbEventTimeMultiplier;
            set {
                if (Set(ref sbEventTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double sbEventTimeOffset;
        public double SbEventTimeOffset {
            get => sbEventTimeOffset;
            set {
                if (Set(ref sbEventTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double sbSampleTimeMultiplier;
        public double SbSampleTimeMultiplier {
            get => sbSampleTimeMultiplier;
            set {
                if (Set(ref sbSampleTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double sbSampleTimeOffset;
        public double SbSampleTimeOffset {
            get => sbSampleTimeOffset;
            set {
                if (Set(ref sbSampleTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double sbSampleVolumeMultiplier;
        public double SbSampleVolumeMultiplier {
            get => sbSampleVolumeMultiplier;
            set => Set(ref sbSampleVolumeMultiplier, value);
        }

        private double sbSampleVolumeOffset;
        public double SbSampleVolumeOffset {
            get => sbSampleVolumeOffset;
            set => Set(ref sbSampleVolumeOffset, value);
        }

        private double breakTimeMultiplier;
        public double BreakTimeMultiplier {
            get => breakTimeMultiplier;
            set {
                if (Set(ref breakTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double breakTimeOffset;
        public double BreakTimeOffset {
            get => breakTimeOffset;
            set {
                if (Set(ref breakTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double videoTimeMultiplier;
        public double VideoTimeMultiplier {
            get => videoTimeMultiplier;
            set {
                if (Set(ref videoTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double videoTimeOffset;
        public double VideoTimeOffset {
            get => videoTimeOffset;
            set {
                if (Set(ref videoTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double previewTimeMultiplier;
        public double PreviewTimeMultiplier {
            get => previewTimeMultiplier;
            set {
                if (Set(ref previewTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double previewTimeOffset;
        public double PreviewTimeOffset {
            get => previewTimeOffset;
            set {
                if (Set(ref previewTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        #endregion

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

        private double[] matchFilter;
        public double[] MatchFilter {
            get => matchFilter;
            set => Set(ref matchFilter, value);
        }

        private double[] unmatchFilter;
        public double[] UnmatchFilter {
            get => unmatchFilter;
            set => Set(ref unmatchFilter, value);
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

        private bool syncTimeFields;
        public bool SyncTimeFields {
            get => syncTimeFields;
            set => Set(ref syncTimeFields, value);
        }

        [JsonIgnore]
        public CommandImplementation ResetCommand { get; set; }

        [JsonIgnore]
        public string[] ExportPaths { get; set; }

        public PropertyTransformerVm() {
            ResetMultipliersAndOffsets();

            SyncTimeFields = false;
            ClipProperties = false;
            EnableFilters = false;
            MatchFilter = new double[0];
            UnmatchFilter = new double[0];
            MinTimeFilter = -1;
            MaxTimeFilter = -1;

            ResetCommand = new CommandImplementation(_ => ResetMultipliersAndOffsets());
        }

        private void ResetMultipliersAndOffsets() {
            TimingpointOffsetMultiplier = 1;
            TimingpointOffsetOffset = 0;
            TimingpointBpmMultiplier = 1;
            TimingpointBpmOffset = 0;
            TimingpointSvMultiplier = 1;
            TimingpointSvOffset = 0;
            TimingpointIndexMultiplier = 1;
            TimingpointIndexOffset = 0;
            TimingpointVolumeMultiplier = 1;
            TimingpointVolumeOffset = 0;
            HitObjectTimeMultiplier = 1;
            HitObjectTimeOffset = 0;
            BookmarkTimeMultiplier = 1;
            BookmarkTimeOffset = 0;
            SbEventTimeMultiplier = 1;
            SbEventTimeOffset = 0;
            SbSampleTimeMultiplier = 1;
            SbSampleTimeOffset = 0;
            BreakTimeMultiplier = 1;
            BreakTimeOffset = 0;
            VideoTimeMultiplier = 1;
            VideoTimeOffset = 0;
            PreviewTimeMultiplier = 1;
            PreviewTimeOffset = 0;
            SbSampleVolumeMultiplier = 1;
            SbSampleVolumeOffset = 0;
        }

        private void SetAllTimeMultipliers(double value) {
            TimingpointOffsetMultiplier = value;
            HitObjectTimeMultiplier = value;
            BookmarkTimeMultiplier = value;
            SbEventTimeMultiplier = value;
            SbSampleTimeMultiplier = value;
            BreakTimeMultiplier = value;
            VideoTimeMultiplier = value;
            PreviewTimeMultiplier = value;
        }

        private void SetAllTimeOffsets(double value) {
            TimingpointOffsetOffset = value;
            HitObjectTimeOffset = value;
            BookmarkTimeOffset = value;
            SbEventTimeOffset = value;
            SbSampleTimeOffset = value;
            BreakTimeOffset = value;
            VideoTimeOffset = value;
            PreviewTimeOffset = value;
        }
    }
}

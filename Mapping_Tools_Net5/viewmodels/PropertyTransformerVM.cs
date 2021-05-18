using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class PropertyTransformerVm : BindableBase{
        #region multipliers and offsets

        private double _timingpointOffsetMultiplier;
        public double TimingpointOffsetMultiplier {
            get => _timingpointOffsetMultiplier;
            set {
                if (Set(ref _timingpointOffsetMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double _timingpointOffsetOffset;
        public double TimingpointOffsetOffset {
            get => _timingpointOffsetOffset;
            set {
                if (Set(ref _timingpointOffsetOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double _timingpointBPMMultiplier;
        public double TimingpointBPMMultiplier {
            get => _timingpointBPMMultiplier;
            set => Set(ref _timingpointBPMMultiplier, value);
        }

        private double _timingpointBPMOffset;
        public double TimingpointBPMOffset {
            get => _timingpointBPMOffset;
            set => Set(ref _timingpointBPMOffset, value);
        }

        private double _timingpointSVMultiplier;
        public double TimingpointSVMultiplier {
            get => _timingpointSVMultiplier;
            set => Set(ref _timingpointSVMultiplier, value);
        }

        private double _timingpointSVOffset;
        public double TimingpointSVOffset {
            get => _timingpointSVOffset;
            set => Set(ref _timingpointSVOffset, value);
        }

        private double _timingpointIndexMultiplier;
        public double TimingpointIndexMultiplier {
            get => _timingpointIndexMultiplier;
            set => Set(ref _timingpointIndexMultiplier, value);
        }

        private double _timingpointIndexOffset;
        public double TimingpointIndexOffset {
            get => _timingpointIndexOffset;
            set => Set(ref _timingpointIndexOffset, value);
        }

        private double _timingpointVolumeMultiplier;
        public double TimingpointVolumeMultiplier {
            get => _timingpointVolumeMultiplier;
            set => Set(ref _timingpointVolumeMultiplier, value);
        }

        private double _timingpointVolumeOffset;
        public double TimingpointVolumeOffset {
            get => _timingpointVolumeOffset;
            set => Set(ref _timingpointVolumeOffset, value);
        }

        private double _hitObjectTimeMultiplier;
        public double HitObjectTimeMultiplier {
            get => _hitObjectTimeMultiplier;
            set {
                if (Set(ref _hitObjectTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double _hitObjectTimeOffset;
        public double HitObjectTimeOffset {
            get => _hitObjectTimeOffset;
            set {
                if (Set(ref _hitObjectTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double _bookmarkTimeMultiplier;
        public double BookmarkTimeMultiplier {
            get => _bookmarkTimeMultiplier;
            set {
                if (Set(ref _bookmarkTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double _bookmarkTimeOffset;
        public double BookmarkTimeOffset {
            get => _bookmarkTimeOffset;
            set {
                if (Set(ref _bookmarkTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double _sbEventTimeMultiplier;
        public double SBEventTimeMultiplier {
            get => _sbEventTimeMultiplier;
            set {
                if (Set(ref _sbEventTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double _sbEventTimeOffset;
        public double SBEventTimeOffset {
            get => _sbEventTimeOffset;
            set {
                if (Set(ref _sbEventTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double _sbSampleTimeMultiplier;
        public double SBSampleTimeMultiplier {
            get => _sbSampleTimeMultiplier;
            set {
                if (Set(ref _sbSampleTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double _sbSampleTimeOffset;
        public double SBSampleTimeOffset {
            get => _sbSampleTimeOffset;
            set {
                if (Set(ref _sbSampleTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double _breakTimeMultiplier;
        public double BreakTimeMultiplier {
            get => _breakTimeMultiplier;
            set {
                if (Set(ref _breakTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double _breakTimeOffset;
        public double BreakTimeOffset {
            get => _breakTimeOffset;
            set {
                if (Set(ref _breakTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double _videoTimeMultiplier;
        public double VideoTimeMultiplier {
            get => _videoTimeMultiplier;
            set {
                if (Set(ref _videoTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double _videoTimeOffset;
        public double VideoTimeOffset {
            get => _videoTimeOffset;
            set {
                if (Set(ref _videoTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        private double _previewTimeMultiplier;
        public double PreviewTimeMultiplier {
            get => _previewTimeMultiplier;
            set {
                if (Set(ref _previewTimeMultiplier, value) && SyncTimeFields) {
                    SetAllTimeMultipliers(value);
                }
            }
        }

        private double _previewTimeOffset;
        public double PreviewTimeOffset {
            get => _previewTimeOffset;
            set {
                if (Set(ref _previewTimeOffset, value) && SyncTimeFields) {
                    SetAllTimeOffsets(value);
                }
            }
        }

        #endregion

        private bool _clipProperties;
        public bool ClipProperties {
            get => _clipProperties;
            set => Set(ref _clipProperties, value);
        }

        private bool _enableFilters;
        public bool EnableFilters {
            get => _enableFilters;
            set => Set(ref _enableFilters, value);
        }

        private double[] _matchFilter;
        public double[] MatchFilter {
            get => _matchFilter;
            set => Set(ref _matchFilter, value);
        }

        private double[] _unmatchFilter;
        public double[] UnmatchFilter {
            get => _unmatchFilter;
            set => Set(ref _unmatchFilter, value);
        }

        private double _minTimeFilter;
        public double MinTimeFilter {
            get => _minTimeFilter;
            set => Set(ref _minTimeFilter, value);
        }

        private double _maxTimeFilter;
        public double MaxTimeFilter {
            get => _maxTimeFilter;
            set => Set(ref _maxTimeFilter, value);
        }

        private bool _syncTimeFields;
        public bool SyncTimeFields {
            get => _syncTimeFields;
            set => Set(ref _syncTimeFields, value);
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
            SBEventTimeMultiplier = 1;
            SBEventTimeOffset = 0;
            SBSampleTimeMultiplier = 1;
            SBSampleTimeOffset = 0;
            BreakTimeMultiplier = 1;
            BreakTimeOffset = 0;
            VideoTimeMultiplier = 1;
            VideoTimeOffset = 0;
            PreviewTimeMultiplier = 1;
            PreviewTimeOffset = 0;
        }

        private void SetAllTimeMultipliers(double value) {
            TimingpointOffsetMultiplier = value;
            HitObjectTimeMultiplier = value;
            BookmarkTimeMultiplier = value;
            SBEventTimeMultiplier = value;
            SBSampleTimeMultiplier = value;
            BreakTimeMultiplier = value;
            VideoTimeMultiplier = value;
            PreviewTimeMultiplier = value;
        }

        private void SetAllTimeOffsets(double value) {
            TimingpointOffsetOffset = value;
            HitObjectTimeOffset = value;
            BookmarkTimeOffset = value;
            SBEventTimeOffset = value;
            SBSampleTimeOffset = value;
            BreakTimeOffset = value;
            VideoTimeOffset = value;
            PreviewTimeOffset = value;
        }
    }
}

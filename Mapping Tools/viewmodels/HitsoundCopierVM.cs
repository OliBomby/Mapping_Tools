using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;

namespace Mapping_Tools.Viewmodels {
    public class HitsoundCopierVm : BindableBase {
        #region Properties

        private string _pathTo;
        private string _pathFrom;
        private int _copyMode;
        private double _temporalLeniency;
        private bool _copyHitsounds;
        private bool _copyBodyHitsounds;
        private bool _copySampleSets;
        private bool _copyVolumes;
        private bool _alwaysPreserve5Volume;
        private bool _copyStoryboardedSamples;
        private bool _ignoreHitsoundSatisfiedSamples;
        private bool _copyToSliderTicks;
        private bool _copyToSliderSlides;
        private bool _muteSliderends;
        private int _snap1;
        private int _snap2;
        private double _minLength;
        private int _mutedIndex;
        private SampleSet _mutedSampleSet;

        public string PathTo {
            get => _pathTo;
            set => Set(ref _pathTo, value);
        }

        public string PathFrom {
            get => _pathFrom;
            set => Set(ref _pathFrom, value);
        }

        public int CopyMode {
            get => _copyMode;
            set {
                if (Set(ref _copyMode, value)) {
                    RaisePropertyChanged(nameof(SmartCopyModeSelected));
                }
            }
        }

        public bool SmartCopyModeSelected => CopyMode == 1;

        public double TemporalLeniency {
            get => _temporalLeniency;
            set => Set(ref _temporalLeniency, value);
        }

        public bool CopyHitsounds {
            get => _copyHitsounds;
            set => Set(ref _copyHitsounds, value);
        }

        public bool CopyBodyHitsounds {
            get => _copyBodyHitsounds;
            set => Set(ref _copyBodyHitsounds, value);
        }

        public bool CopySampleSets {
            get => _copySampleSets;
            set => Set(ref _copySampleSets, value);
        }

        public bool CopyVolumes {
            get => _copyVolumes;
            set => Set(ref _copyVolumes, value);
        }

        public bool AlwaysPreserve5Volume {
            get => _alwaysPreserve5Volume;
            set => Set(ref _alwaysPreserve5Volume, value);
        }

        public bool CopyStoryboardedSamples {
            get => _copyStoryboardedSamples;
            set => Set(ref _copyStoryboardedSamples, value);
        }

        public bool IgnoreHitsoundSatisfiedSamples {
            get => _ignoreHitsoundSatisfiedSamples;
            set => Set(ref _ignoreHitsoundSatisfiedSamples, value);
        }

        public bool CopyToSliderTicks {
            get => _copyToSliderTicks;
            set => Set(ref _copyToSliderTicks, value);
        }

        public bool CopyToSliderSlides {
            get => _copyToSliderSlides;
            set => Set(ref _copyToSliderSlides, value);
        }

        public bool MuteSliderends {
            get => _muteSliderends;
            set => Set(ref _muteSliderends, value);
        }

        public int Snap1 {
            get => _snap1;
            set => Set(ref _snap1, value);
        }

        public int Snap2 {
            get => _snap2;
            set => Set(ref _snap2, value);
        }

        public string[] Snaps1 => new[] { "1/1", "1/2", "1/4", "1/8", "1/16" };

        public string[] Snaps2 => new[] { "1/1", "1/3", "1/6", "1/12" };

        public double MinLength {
            get => _minLength;
            set => Set(ref _minLength, value);
        }

        public int MutedIndex {
            get => _mutedIndex;
            set => Set(ref _mutedIndex, value);
        }

        public SampleSet MutedSampleSet {
            get => _mutedSampleSet;
            set => Set(ref _mutedSampleSet, value);
        }

        public IEnumerable<SampleSet> MutedSampleSets => Enum.GetValues(typeof(SampleSet)).Cast<SampleSet>();

        public CommandImplementation ImportLoadCommand { get; }
        public CommandImplementation ImportBrowseCommand { get; }
        public CommandImplementation ExportLoadCommand { get; }
        public CommandImplementation ExportBrowseCommand { get; }

        #endregion

        public HitsoundCopierVm() {
            PathFrom = string.Empty;
            PathTo = string.Empty;
            CopyMode = 0;
            TemporalLeniency = 5;
            CopyHitsounds = true;
            CopyBodyHitsounds = true;
            CopySampleSets = true;
            CopyVolumes = true;
            AlwaysPreserve5Volume = true;
            CopyStoryboardedSamples = false;
            IgnoreHitsoundSatisfiedSamples = true;
            CopyToSliderTicks = false;
            CopyToSliderSlides = false;
            MuteSliderends = false;
            Snap1 = 4;
            Snap2 = 6;
            MinLength = 0.5;
            MutedIndex = -1;
            MutedSampleSet = SampleSet.Soft;

            ImportLoadCommand = new CommandImplementation(
                _ => {
                    string path = IOHelper.GetCurrentBeatmap();
                    if (path != "") {
                        PathFrom = path;
                    }
                });

            ImportBrowseCommand = new CommandImplementation(
                _ => {
                    string[] paths = IOHelper.BeatmapFileDialog(restore: !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                    if (paths.Length != 0) {
                        PathFrom = paths[0];
                    }
                });

            ExportLoadCommand = new CommandImplementation(
                _ => {
                    string path = IOHelper.GetCurrentBeatmap();
                    if (path != "") {
                        PathTo = path;
                    }
                });

            ExportBrowseCommand = new CommandImplementation(
                _ => {
                    string[] paths = IOHelper.BeatmapFileDialog(true, !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                    if (paths.Length != 0) {
                        PathTo = string.Join("|", paths);
                    }
                });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
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
        public int _startIndex;
        private bool _muteSliderends;
        private IBeatDivisor[] _beatDivisors;
        private IBeatDivisor[] _mutedDivisors;
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
            set { 
                if (Set(ref _copyToSliderTicks, value)) {
                    RaisePropertyChanged(nameof(StartIndexBoxVisible));
                } 
            }
        }

        public bool CopyToSliderSlides {
            get => _copyToSliderSlides;
            set {
                if (Set(ref _copyToSliderSlides, value)) {
                    RaisePropertyChanged(nameof(StartIndexBoxVisible));
                }
            }
        }

        public bool StartIndexBoxVisible => CopyToSliderSlides || CopyToSliderTicks;

        public int StartIndex {
            get => _startIndex;
            set => Set(ref _startIndex, value);
        }

        public bool MuteSliderends {
            get => _muteSliderends;
            set => Set(ref _muteSliderends, value);
        }

        public IBeatDivisor[] BeatDivisors {
            get => _beatDivisors;
            set => Set(ref _beatDivisors, value);
        }

        public IBeatDivisor[] MutedDivisors {
            get => _mutedDivisors;
            set => Set(ref _mutedDivisors, value);
        }

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
            StartIndex = 100;
            MuteSliderends = false;
            BeatDivisors = new IBeatDivisor[] {
                new RationalBeatDivisor(1),
                new RationalBeatDivisor(4), new RationalBeatDivisor(3),
                new RationalBeatDivisor(8), new RationalBeatDivisor(6),
                new RationalBeatDivisor(16), new RationalBeatDivisor(12)
            };
            MutedDivisors = new IBeatDivisor[] {
                new RationalBeatDivisor(4), new RationalBeatDivisor(3),
                new RationalBeatDivisor(8), new RationalBeatDivisor(6),
                new RationalBeatDivisor(16), new RationalBeatDivisor(12)
            };
            MinLength = 0.5;
            MutedIndex = -1;
            MutedSampleSet = SampleSet.Auto;

            ImportLoadCommand = new CommandImplementation(
                _ => {
                    try {
                        string path = IOHelper.GetCurrentBeatmap();
                        if (path != "") {
                            PathFrom = path;
                        }
                    }
                    catch (Exception ex) {
                        ex.Show();
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
                    try {
                        string path = IOHelper.GetCurrentBeatmap();
                        if (path != "") {
                            PathTo = path;
                        }
                    } catch (Exception ex) {
                        ex.Show();
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

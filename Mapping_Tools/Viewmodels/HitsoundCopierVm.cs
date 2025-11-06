using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json.Serialization;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;

namespace Mapping_Tools.Viewmodels {
    public class HitsoundCopierVm : BindableBase {
        #region Properties

        private string pathTo;
        private string pathFrom;
        private int copyMode;
        private double temporalLeniency;
        private bool copyHitsounds;
        private bool copyBodyHitsounds;
        private bool copySampleSets;
        private bool copyVolumes;
        private bool alwaysPreserve5Volume;
        private bool copyStoryboardedSamples;
        private bool ignoreHitsoundSatisfiedSamples;
        private bool ignoreWheneverHitsound;
        private bool copyToSliderTicks;
        private bool copyToSliderSlides;
        private int startIndex;
        private bool muteSliderends;
        private IBeatDivisor[] beatDivisors;
        private IBeatDivisor[] mutedDivisors;
        private double minLength;
        private int mutedIndex;
        private SampleSet mutedSampleSet;

        public string PathTo {
            get => pathTo;
            set => Set(ref pathTo, value);
        }

        public string PathFrom {
            get => pathFrom;
            set => Set(ref pathFrom, value);
        }

        public int CopyMode {
            get => copyMode;
            set {
                if (Set(ref copyMode, value)) {
                    RaisePropertyChanged(nameof(SmartCopyModeSelected));
                }
            }
        }

        [JsonIgnore]
        public bool SmartCopyModeSelected => CopyMode == 1;

        public double TemporalLeniency {
            get => temporalLeniency;
            set => Set(ref temporalLeniency, value);
        }

        public bool CopyHitsounds {
            get => copyHitsounds;
            set => Set(ref copyHitsounds, value);
        }

        public bool CopyBodyHitsounds {
            get => copyBodyHitsounds;
            set => Set(ref copyBodyHitsounds, value);
        }

        public bool CopySampleSets {
            get => copySampleSets;
            set => Set(ref copySampleSets, value);
        }

        public bool CopyVolumes {
            get => copyVolumes;
            set => Set(ref copyVolumes, value);
        }

        public bool AlwaysPreserve5Volume {
            get => alwaysPreserve5Volume;
            set => Set(ref alwaysPreserve5Volume, value);
        }

        public bool CopyStoryboardedSamples {
            get => copyStoryboardedSamples;
            set => Set(ref copyStoryboardedSamples, value);
        }

        public bool IgnoreHitsoundSatisfiedSamples {
            get => ignoreHitsoundSatisfiedSamples;
            set => Set(ref ignoreHitsoundSatisfiedSamples, value);
        }

        public bool IgnoreWheneverHitsound {
            get => ignoreWheneverHitsound;
            set => Set(ref ignoreWheneverHitsound, value);
        }

        public bool CopyToSliderTicks {
            get => copyToSliderTicks;
            set { 
                if (Set(ref copyToSliderTicks, value)) {
                    RaisePropertyChanged(nameof(StartIndexBoxVisible));
                } 
            }
        }

        public bool CopyToSliderSlides {
            get => copyToSliderSlides;
            set {
                if (Set(ref copyToSliderSlides, value)) {
                    RaisePropertyChanged(nameof(StartIndexBoxVisible));
                }
            }
        }

        [JsonIgnore]
        public bool StartIndexBoxVisible => CopyToSliderSlides || CopyToSliderTicks;

        public int StartIndex {
            get => startIndex;
            set => Set(ref startIndex, value);
        }

        public bool MuteSliderends {
            get => muteSliderends;
            set => Set(ref muteSliderends, value);
        }

        public IBeatDivisor[] BeatDivisors {
            get => beatDivisors;
            set => Set(ref beatDivisors, value);
        }

        public IBeatDivisor[] MutedDivisors {
            get => mutedDivisors;
            set => Set(ref mutedDivisors, value);
        }

        public double MinLength {
            get => minLength;
            set => Set(ref minLength, value);
        }

        public int MutedIndex {
            get => mutedIndex;
            set => Set(ref mutedIndex, value);
        }

        public SampleSet MutedSampleSet {
            get => mutedSampleSet;
            set => Set(ref mutedSampleSet, value);
        }

        [JsonIgnore]
        public IEnumerable<SampleSet> MutedSampleSets => Enum.GetValues(typeof(SampleSet)).Cast<SampleSet>();

        [JsonIgnore]
        public CommandImplementation ImportLoadCommand { get; }
        [JsonIgnore]
        public CommandImplementation ImportBrowseCommand { get; }
        [JsonIgnore]
        public CommandImplementation ExportLoadCommand { get; }
        [JsonIgnore]
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
            MutedSampleSet = SampleSet.None;

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
                    string pathFromDirectory = Directory.GetParent(PathFrom).FullName;

                    string[] paths = IOHelper.BeatmapFileDialog(pathFromDirectory, true);
                    if (paths.Length != 0) {
                        PathTo = string.Join("|", paths);
                    }
                });
        }
    }
}

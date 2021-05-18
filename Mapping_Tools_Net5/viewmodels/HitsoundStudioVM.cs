using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Viewmodels {
    public class HitsoundStudioVm : BindableBase {
        private string _baseBeatmap;
        public string BaseBeatmap {
            get => _baseBeatmap;
            set => Set(ref _baseBeatmap, value);
        }

        private Sample _defaultSample;
        public Sample DefaultSample {
            get => _defaultSample;
            set => Set(ref _defaultSample, value);
        }

        private string _exportFolder;
        public string ExportFolder {
            get => _exportFolder;
            set => Set(ref _exportFolder, value);
        }

        private string _hitsoundDiffName;
        public string HitsoundDiffName {
            get => _hitsoundDiffName;
            set => Set(ref _hitsoundDiffName, value);
        }

        private bool _showResults;
        public bool ShowResults {
            get => _showResults;
            set => Set(ref _showResults, value);
        }

        private bool _exportMap;
        public bool ExportMap {
            get => _exportMap;
            set => Set(ref _exportMap, value);
        }

        private bool _exportSamples;
        public bool ExportSamples {
            get => _exportSamples;
            set => Set(ref _exportSamples, value);
        }

        private bool _deleteAllInExportFirst;
        public bool DeleteAllInExportFirst {
            get => _deleteAllInExportFirst;
            set => Set(ref _deleteAllInExportFirst, value);
        }

        private bool _usePreviousSampleSchema;
        public bool UsePreviousSampleSchema {
            get => _usePreviousSampleSchema;
            set => Set(ref _usePreviousSampleSchema, value);
        }

        private bool _allowGrowthPreviousSampleSchema;
        public bool AllowGrowthPreviousSampleSchema {
            get => _allowGrowthPreviousSampleSchema;
            set => Set(ref _allowGrowthPreviousSampleSchema, value);
        }

        private bool _addCoincidingRegularHitsounds;
        public bool AddCoincidingRegularHitsounds {
            get => _addCoincidingRegularHitsounds;
            set => Set(ref _addCoincidingRegularHitsounds, value);
        }

        public SampleSchema PreviousSampleSchema { get; set; }

        private HitsoundExportMode _hitsoundExportModeSetting;
        public HitsoundExportMode HitsoundExportModeSetting {
            get => _hitsoundExportModeSetting;
            set {
                if (Set(ref _hitsoundExportModeSetting, value)) {
                    RaisePropertyChanged(nameof(StandardExtraSettingsVisibility));
                    RaisePropertyChanged(nameof(CoincidingExtraSettingsVisibility));
                    RaisePropertyChanged(nameof(StoryboardExtraSettingsVisibility));
                }
            }
        }

        public Visibility StandardExtraSettingsVisibility =>
            HitsoundExportModeSetting == HitsoundExportMode.Standard ? Visibility.Visible : Visibility.Collapsed;

        public Visibility CoincidingExtraSettingsVisibility =>
            HitsoundExportModeSetting == HitsoundExportMode.Coinciding ? Visibility.Visible : Visibility.Collapsed;

        public Visibility StoryboardExtraSettingsVisibility =>
            HitsoundExportModeSetting == HitsoundExportMode.Storyboard ? Visibility.Visible : Visibility.Collapsed;
        
        public IEnumerable<HitsoundExportMode> HitsoundExportModes => Enum.GetValues(typeof(HitsoundExportMode)).Cast<HitsoundExportMode>();

        private GameMode _hitsoundExportGameMode;
        public GameMode HitsoundExportGameMode {
            get => _hitsoundExportGameMode;
            set => Set(ref _hitsoundExportGameMode, value);
        }
        
        public IEnumerable<GameMode> HitsoundExportGameModes => Enum.GetValues(typeof(GameMode)).Cast<GameMode>();

        private double _zipLayersLeniency;
        public double ZipLayersLeniency {
            get => _zipLayersLeniency;
            set => Set(ref _zipLayersLeniency, value);
        }

        private int _firstCustomIndex;
        public int FirstCustomIndex {
            get => _firstCustomIndex;
            set => Set(ref _firstCustomIndex, value);
        }

        private HitsoundExporter.SampleExportFormat _singleSampleExportFormat;
        public HitsoundExporter.SampleExportFormat SingleSampleExportFormat {
            get => _singleSampleExportFormat;
            set {
                if (Set(ref _singleSampleExportFormat, value)) {
                    RaisePropertyChanged(nameof(SingleSampleExportFormatDisplay));
                    if (value == HitsoundExporter.SampleExportFormat.MidiChords) {
                        MixedSampleExportFormat = value;
                    } else if (MixedSampleExportFormat == HitsoundExporter.SampleExportFormat.MidiChords) {
                        MixedSampleExportFormat = value;
                    }
                }
            }
        }

        private HitsoundExporter.SampleExportFormat _mixedSampleExportFormat;
        public HitsoundExporter.SampleExportFormat MixedSampleExportFormat {
            get => _mixedSampleExportFormat;
            set {
                if (Set(ref _mixedSampleExportFormat, value)) {
                    RaisePropertyChanged(nameof(MixedSampleExportFormatDisplay));
                    if (value == HitsoundExporter.SampleExportFormat.MidiChords) {
                        SingleSampleExportFormat = value;
                    } else if (SingleSampleExportFormat == HitsoundExporter.SampleExportFormat.MidiChords) {
                        SingleSampleExportFormat = value;
                    }
                }
            }
        }

        public readonly Dictionary<HitsoundExporter.SampleExportFormat, string> SampleExportFormatDisplayNameMapping = 
            new Dictionary<HitsoundExporter.SampleExportFormat, string> {{HitsoundExporter.SampleExportFormat.Default, "Default"}, 
                {HitsoundExporter.SampleExportFormat.WaveIeeeFloat, "IEEE Float (.wav)"},
                {HitsoundExporter.SampleExportFormat.WavePcm, "PCM 16-bit (.wav)"},
                {HitsoundExporter.SampleExportFormat.OggVorbis, "Vorbis (.ogg)"},
                {HitsoundExporter.SampleExportFormat.MidiChords, "Single-chord Midi (.mid)"}
            };

        public IEnumerable<string> SampleExportFormatDisplayNames => SampleExportFormatDisplayNameMapping.Values;

        public string SingleSampleExportFormatDisplay {
            get => SampleExportFormatDisplayNameMapping[SingleSampleExportFormat];
            set {
                foreach (var kvp in SampleExportFormatDisplayNameMapping.Where(kvp => kvp.Value == value)) {
                    SingleSampleExportFormat = kvp.Key;
                    break;
                }
            }
        }

        public string MixedSampleExportFormatDisplay {
            get => SampleExportFormatDisplayNameMapping[MixedSampleExportFormat];
            set {
                foreach (var kvp in SampleExportFormatDisplayNameMapping.Where(kvp => kvp.Value == value)) {
                    MixedSampleExportFormat = kvp.Key;
                    break;
                }
            }
        }

        public ObservableCollection<HitsoundLayer> HitsoundLayers { get; set; }

        public string EditTimes { get; set; }

        public HitsoundStudioVm() : this("", new Sample {Priority = int.MaxValue}, new ObservableCollection<HitsoundLayer>()) { }

        public HitsoundStudioVm(string baseBeatmap, Sample defaultSample, ObservableCollection<HitsoundLayer> hitsoundLayers) {
            BaseBeatmap = baseBeatmap;
            DefaultSample = defaultSample;
            HitsoundLayers = hitsoundLayers;
            ExportFolder = MainWindow.ExportPath;
            HitsoundDiffName = "Hitsounds";
            ShowResults = false;
            ExportMap = true;
            ExportSamples = true;
            DeleteAllInExportFirst = false;
            AddCoincidingRegularHitsounds = true;
            HitsoundExportModeSetting = HitsoundExportMode.Standard;
            HitsoundExportGameMode = GameMode.Standard;
            ZipLayersLeniency = 15;
            FirstCustomIndex = 1;
            SingleSampleExportFormat = HitsoundExporter.SampleExportFormat.Default;
            MixedSampleExportFormat = HitsoundExporter.SampleExportFormat.Default;
        }

        public enum HitsoundExportMode {
            Standard,
            Coinciding,
            Storyboard
        }
    }
}

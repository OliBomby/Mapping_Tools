using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels;

public class HitsoundStudioVm : BindableBase {
    private string baseBeatmap;
    public string BaseBeatmap {
        get => baseBeatmap;
        set => Set(ref baseBeatmap, value);
    }

    private Sample defaultSample;
    public Sample DefaultSample {
        get => defaultSample;
        set => Set(ref defaultSample, value);
    }

    private string exportFolder;
    public string ExportFolder {
        get => exportFolder;
        set => Set(ref exportFolder, value);
    }

    private string hitsoundDiffName;
    public string HitsoundDiffName {
        get => hitsoundDiffName;
        set => Set(ref hitsoundDiffName, value);
    }

    private bool showResults;
    public bool ShowResults {
        get => showResults;
        set => Set(ref showResults, value);
    }

    private bool exportMap;
    public bool ExportMap {
        get => exportMap;
        set => Set(ref exportMap, value);
    }

    private bool exportSamples;
    public bool ExportSamples {
        get => exportSamples;
        set => Set(ref exportSamples, value);
    }

    private bool deleteAllInExportFirst;
    public bool DeleteAllInExportFirst {
        get => deleteAllInExportFirst;
        set => Set(ref deleteAllInExportFirst, value);
    }

    private bool usePreviousSampleSchema;
    public bool UsePreviousSampleSchema {
        get => usePreviousSampleSchema;
        set => Set(ref usePreviousSampleSchema, value);
    }

    private bool allowGrowthPreviousSampleSchema;
    public bool AllowGrowthPreviousSampleSchema {
        get => allowGrowthPreviousSampleSchema;
        set => Set(ref allowGrowthPreviousSampleSchema, value);
    }

    private bool addCoincidingRegularHitsounds;
    public bool AddCoincidingRegularHitsounds {
        get => addCoincidingRegularHitsounds;
        set => Set(ref addCoincidingRegularHitsounds, value);
    }

    private bool addGreenLineVolumeToMidi;
    public bool AddGreenLineVolumeToMidi {
        get => addGreenLineVolumeToMidi;
        set => Set(ref addGreenLineVolumeToMidi, value);
    }

    public SampleSchema PreviousSampleSchema { get; set; }

    private HitsoundExportMode hitsoundExportModeSetting;
    public HitsoundExportMode HitsoundExportModeSetting {
        get => hitsoundExportModeSetting;
        set {
            if (Set(ref hitsoundExportModeSetting, value)) {
                RaisePropertyChanged(nameof(StandardExtraSettingsVisibility));
                RaisePropertyChanged(nameof(CoincidingExtraSettingsVisibility));
                RaisePropertyChanged(nameof(StoryboardExtraSettingsVisibility));
                RaisePropertyChanged(nameof(MidiExtraSettingsVisibility));
                RaisePropertyChanged(nameof(SampleExportSettingsVisibility));
            }
        }
    }

    [JsonIgnore]
    public Visibility StandardExtraSettingsVisibility =>
        HitsoundExportModeSetting == HitsoundExportMode.Standard ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility CoincidingExtraSettingsVisibility =>
        HitsoundExportModeSetting == HitsoundExportMode.Coinciding ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility StoryboardExtraSettingsVisibility =>
        HitsoundExportModeSetting == HitsoundExportMode.Storyboard ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility MidiExtraSettingsVisibility =>
        HitsoundExportModeSetting == HitsoundExportMode.Midi ? Visibility.Visible : Visibility.Collapsed;

    [JsonIgnore]
    public Visibility SampleExportSettingsVisibility =>
        HitsoundExportModeSetting == HitsoundExportMode.Midi ? Visibility.Collapsed : Visibility.Visible;
        
    public IEnumerable<HitsoundExportMode> HitsoundExportModes => Enum.GetValues(typeof(HitsoundExportMode)).Cast<HitsoundExportMode>();

    private GameMode hitsoundExportGameMode;
    public GameMode HitsoundExportGameMode {
        get => hitsoundExportGameMode;
        set => Set(ref hitsoundExportGameMode, value);
    }

    [JsonIgnore]
    public IEnumerable<GameMode> HitsoundExportGameModes => Enum.GetValues(typeof(GameMode)).Cast<GameMode>();

    private double zipLayersLeniency;
    public double ZipLayersLeniency {
        get => zipLayersLeniency;
        set => Set(ref zipLayersLeniency, value);
    }

    private int firstCustomIndex;
    public int FirstCustomIndex {
        get => firstCustomIndex;
        set => Set(ref firstCustomIndex, value);
    }

    private HitsoundExporter.SampleExportFormat singleSampleExportFormat;
    public HitsoundExporter.SampleExportFormat SingleSampleExportFormat {
        get => singleSampleExportFormat;
        set {
            if (Set(ref singleSampleExportFormat, value)) {
                RaisePropertyChanged(nameof(SingleSampleExportFormatDisplay));
                if (value == HitsoundExporter.SampleExportFormat.MidiChords) {
                    MixedSampleExportFormat = value;
                } else if (MixedSampleExportFormat == HitsoundExporter.SampleExportFormat.MidiChords) {
                    MixedSampleExportFormat = value;
                }
            }
        }
    }

    private HitsoundExporter.SampleExportFormat mixedSampleExportFormat;
    public HitsoundExporter.SampleExportFormat MixedSampleExportFormat {
        get => mixedSampleExportFormat;
        set {
            if (Set(ref mixedSampleExportFormat, value)) {
                RaisePropertyChanged(nameof(MixedSampleExportFormatDisplay));
                if (value == HitsoundExporter.SampleExportFormat.MidiChords) {
                    SingleSampleExportFormat = value;
                } else if (SingleSampleExportFormat == HitsoundExporter.SampleExportFormat.MidiChords) {
                    SingleSampleExportFormat = value;
                }
            }
        }
    }

    [JsonIgnore]
    public readonly Dictionary<HitsoundExporter.SampleExportFormat, string> SampleExportFormatDisplayNameMapping = 
        new Dictionary<HitsoundExporter.SampleExportFormat, string> {{HitsoundExporter.SampleExportFormat.Default, "Default"}, 
            {HitsoundExporter.SampleExportFormat.WaveIeeeFloat, "IEEE Float (.wav)"},
            {HitsoundExporter.SampleExportFormat.WavePcm, "PCM 16-bit (.wav)"},
            {HitsoundExporter.SampleExportFormat.OggVorbis, "Vorbis (.ogg)"},
            {HitsoundExporter.SampleExportFormat.MidiChords, "Single-chord MIDI (.mid)"}
        };

    [JsonIgnore]
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
        AddGreenLineVolumeToMidi = true;
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
        Storyboard,
        Midi,
    }
}
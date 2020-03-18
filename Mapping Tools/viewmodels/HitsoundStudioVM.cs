using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;

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

        public List<CustomIndex> PreviousSampleSchema { get; set; }

        private HitsoundExportMode hitsoundExportModeSetting;
        public HitsoundExportMode HitsoundExportModeSetting {
            get => hitsoundExportModeSetting;
            set => Set(ref hitsoundExportModeSetting, value);
        }
        
        public IEnumerable<HitsoundExportMode> HitsoundExportModes => Enum.GetValues(typeof(HitsoundExportMode)).Cast<HitsoundExportMode>();

        private GameMode hitsoundExportGameMode;
        public GameMode HitsoundExportGameMode {
            get => hitsoundExportGameMode;
            set => Set(ref hitsoundExportGameMode, value);
        }
        
        public IEnumerable<GameMode> HitsoundExportGameModes => Enum.GetValues(typeof(GameMode)).Cast<GameMode>();

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
        }

        public enum HitsoundExportMode {
            Standard,
            Coinciding,
            Storyboard
        }
    }
}

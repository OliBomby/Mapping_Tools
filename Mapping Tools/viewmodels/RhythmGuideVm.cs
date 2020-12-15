using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Mapping_Tools_Core;
using Mapping_Tools_Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.Tools.RhythmGuide;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mapping_Tools.Viewmodels {

    public class RhythmGuideVm : BindableBase {
        #region private_members

        private string[] _paths;
        private GameMode _outputGameMode;
        private string _outputName;
        private bool _ncEverything;
        private SelectionMode _selectionMode;
        private IBeatDivisor[] _beatDivisors;
        private ExportMode _exportMode;
        private string _exportPath;

        #endregion

        /// <summary>
        /// A string of paths to import from.
        /// </summary>
        public string[] Paths {
            get => _paths;
            set => Set(ref _paths, value);
        }

        /// <summary>
        /// The Selected output game mode
        /// </summary>
        public GameMode OutputGameMode {
            get => _outputGameMode;
            set => Set(ref _outputGameMode, value);
        }

        /// <summary>
        /// The difficulty name of the output
        /// </summary>
        public string OutputName {
            get => _outputName;
            set => Set(ref _outputName, value);
        }

        /// <summary>
        /// If each object should have a new combo.
        /// </summary>
        public bool NcEverything {
            get => _ncEverything;
            set => Set(ref _ncEverything, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public SelectionMode SelectionMode {
            get => _selectionMode;
            set => Set(ref _selectionMode, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public ExportMode ExportMode {
            get => _exportMode;
            set => Set(ref _exportMode, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string ExportPath {
            get => _exportPath;
            set => Set(ref _exportPath, value);
        }

        public IBeatDivisor[] BeatDivisors {
            get => _beatDivisors;
            set => Set(ref _beatDivisors, value);
        }

        public RhythmGuideVm() {
            _paths = new string[0];
            _outputGameMode = GameMode.Standard;
            _outputName = "Hitsounds";
            _ncEverything = false;
            _selectionMode = SelectionMode.HitsoundEvents;
            _beatDivisors = RationalBeatDivisor.GetDefaultBeatDivisors();
            _exportMode = ExportMode.NewMap;
            _exportPath = Path.Combine(MainWindow.ExportPath, @"rhythm_guide.osu");

            ImportLoadCommand = new CommandImplementation(
                _ => {
                    try {
                        var path = IOHelper.GetCurrentBeatmap();
                        if (path != "") {
                            Paths = new[] { path };
                        }
                    }
                    catch (Exception ex) {
                        ex.Show();
                    }
                });

            ImportBrowseCommand = new CommandImplementation(
                _ => {
                    var paths = IOHelper.BeatmapFileDialog(true, !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                    if (paths.Length != 0) {
                        Paths = paths;
                    }
                });

            ExportLoadCommand = new CommandImplementation(
                _ => {
                    try {
                        var path = IOHelper.GetCurrentBeatmap();
                        if (path != "") {
                            ExportPath = path;
                        }
                    }
                    catch (Exception ex) {
                        ex.Show();
                    }
                });

            ExportBrowseCommand = new CommandImplementation(
                _ => {
                    var paths = IOHelper.BeatmapFileDialog(restore: !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                    if (paths.Length != 0) {
                        ExportPath = paths[0];
                    }
                });
        }

        [JsonIgnore]
        public CommandImplementation ImportLoadCommand { get; }
        [JsonIgnore]
        public CommandImplementation ImportBrowseCommand { get; }
        [JsonIgnore]
        public CommandImplementation ExportLoadCommand { get; }
        [JsonIgnore]
        public CommandImplementation ExportBrowseCommand { get; }

        [JsonIgnore]
        public IEnumerable<ExportMode> ExportModes => Enum.GetValues(typeof(ExportMode)).Cast<ExportMode>();
        [JsonIgnore]
        public IEnumerable<GameMode> ExportGameModes => Enum.GetValues(typeof(GameMode)).Cast<GameMode>();
        [JsonIgnore]
        public IEnumerable<SelectionMode> SelectionModes => Enum.GetValues(typeof(SelectionMode)).Cast<SelectionMode>();
    }
}
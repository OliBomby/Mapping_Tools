using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {

    public class RhythmGuideVm : BindableBase {
        public RhythmGuide.RhythmGuideGeneratorArgs GuideGeneratorArgs { set; get; }

        public RhythmGuideVm() {
            GuideGeneratorArgs = new RhythmGuide.RhythmGuideGeneratorArgs();

            ImportLoadCommand = new CommandImplementation(
                _ => {
                    try {
                        var path = IOHelper.GetCurrentBeatmap();
                        if (path != "") {
                            GuideGeneratorArgs.Paths = new[] { path };
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
                        GuideGeneratorArgs.Paths = paths;
                    }
                });

            ExportLoadCommand = new CommandImplementation(
                _ => {
                    try {
                        var path = IOHelper.GetCurrentBeatmap();
                        if (path != "") {
                            GuideGeneratorArgs.ExportPath = path;
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
                        GuideGeneratorArgs.ExportPath = paths[0];
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
        public IEnumerable<RhythmGuide.ExportMode> ExportModes => Enum.GetValues(typeof(RhythmGuide.ExportMode)).Cast<RhythmGuide.ExportMode>();
        [JsonIgnore]
        public IEnumerable<GameMode> ExportGameModes => Enum.GetValues(typeof(GameMode)).Cast<GameMode>();
        [JsonIgnore]
        public IEnumerable<RhythmGuide.SelectionMode> SelectionModes => Enum.GetValues(typeof(RhythmGuide.SelectionMode)).Cast<RhythmGuide.SelectionMode>();
    }
}
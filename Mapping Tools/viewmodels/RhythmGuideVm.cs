using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Viewmodels {

    public class RhythmGuideVm : BindableBase {
        public RhythmGuide.RhythmGuideGeneratorArgs GuideGeneratorArgs { get; }

        public RhythmGuideVm() {
            GuideGeneratorArgs = new RhythmGuide.RhythmGuideGeneratorArgs();

            ImportLoadCommand = new CommandImplementation(
                _ => {
                    var path = IOHelper.GetCurrentBeatmap();
                    if (path != "") {
                        GuideGeneratorArgs.Paths = new[] {path};
                    }
                });

            ImportBrowseCommand = new CommandImplementation(
                _ => {
                    var paths = IOHelper.BeatmapFileDialog(true, true);
                    if (paths.Length != 0) {
                        GuideGeneratorArgs.Paths = paths;
                    }
                });

            ExportLoadCommand = new CommandImplementation(
                _ => {
                    var path = IOHelper.GetCurrentBeatmap();
                    if (path != "") {
                        GuideGeneratorArgs.ExportPath = path;
                    }
                });

            ExportBrowseCommand = new CommandImplementation(
                _ => {
                    var paths = IOHelper.BeatmapFileDialog(restore: true);
                    if (paths.Length != 0) {
                        GuideGeneratorArgs.ExportPath = paths[0];
                    }
                });
        }

        public CommandImplementation ImportLoadCommand { get; }
        public CommandImplementation ImportBrowseCommand { get; }
        public CommandImplementation ExportLoadCommand { get; }
        public CommandImplementation ExportBrowseCommand { get; }

        public IEnumerable<RhythmGuide.ExportMode> ExportModes => Enum.GetValues(typeof(RhythmGuide.ExportMode)).Cast<RhythmGuide.ExportMode>();
        public IEnumerable<GameMode> ExportGameModes => Enum.GetValues(typeof(GameMode)).Cast<GameMode>();
        public IEnumerable<RhythmGuide.SelectionMode> SelectionModes => Enum.GetValues(typeof(RhythmGuide.SelectionMode)).Cast<RhythmGuide.SelectionMode>();
    }
}
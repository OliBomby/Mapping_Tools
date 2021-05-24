using System.ComponentModel;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class PatternCodeImportVm : BindableBase {
        private string _name = string.Empty;
        private string _hitObjects = string.Empty;
        private string _timingPoints = string.Empty;
        private double _globalSv = 1.4;
        private GameMode _gameMode = GameMode.Standard;

        [DisplayName("Name")]
        [Description("The name for the pattern.")]
        public string Name { 
            get => _name; 
            set => Set(ref _name, value);
        }

        [MultiLineInput]
        [TextWrapping(TextWrapping.NoWrap)]
        [DisplayName("Hit objects")]
        [Description("The hit objects for the pattern.")]
        public string HitObjects {
            get => _hitObjects;
            set => Set(ref _hitObjects, value);
        }

        [MultiLineInput]
        [TextWrapping(TextWrapping.NoWrap)]
        [DisplayName("Timing points")]
        [Description("The timing points for the pattern. Tip: Include a redline so timing scaling works during export.")]
        public string TimingPoints {
            get => _timingPoints;
            set => Set(ref _timingPoints, value);
        }

        [DisplayName("Global SV")]
        [Description("The global slider multiplier for the pattern.")]
        public double GlobalSv {
            get => _globalSv;
            set => Set(ref _globalSv, value);
        }

        [DisplayName("Game mode")]
        [Description("The game mode for the pattern.")]
        public GameMode GameMode {
            get => _gameMode;
            set => Set(ref _gameMode, value);
        }
    }
}
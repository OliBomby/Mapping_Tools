using System.ComponentModel;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class PatternCodeImportVm : BindableBase {
        private string name = string.Empty;
        private string hitObjects = string.Empty;
        private string timingPoints = string.Empty;
        private double globalSv = 1.4;
        private GameMode gameMode = GameMode.Standard;

        [DisplayName("Name")]
        [Description("The name for the pattern.")]
        public string Name { 
            get => name; 
            set => Set(ref name, value);
        }

        [MultiLineInput]
        [TextWrapping(TextWrapping.NoWrap)]
        [DisplayName("Hit objects")]
        [Description("The hit objects for the pattern.")]
        public string HitObjects {
            get => hitObjects;
            set => Set(ref hitObjects, value);
        }

        [MultiLineInput]
        [TextWrapping(TextWrapping.NoWrap)]
        [DisplayName("Timing points")]
        [Description("The timing points for the pattern. Tip: Include a redline so timing scaling works during export.")]
        public string TimingPoints {
            get => timingPoints;
            set => Set(ref timingPoints, value);
        }

        [DisplayName("Global SV")]
        [Description("The global slider multiplier for the pattern.")]
        public double GlobalSv {
            get => globalSv;
            set => Set(ref globalSv, value);
        }

        [DisplayName("Game mode")]
        [Description("The game mode for the pattern.")]
        public GameMode GameMode {
            get => gameMode;
            set => Set(ref gameMode, value);
        }
    }
}
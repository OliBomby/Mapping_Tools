using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    /// <summary>
    /// Auto-fail detector View Model
    /// </summary>
    public class AutoFailDetectorVm : BindableBase {
        [JsonIgnore]
        public string[] Paths { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        private bool showUnloadingObjects = true;
        public bool ShowUnloadingObjects {
            get => showUnloadingObjects;
            set => Set(ref showUnloadingObjects, value);
        }

        private bool showPotentialUnloadingObjects;
        public bool ShowPotentialUnloadingObjects {
            get => showPotentialUnloadingObjects;
            set => Set(ref showPotentialUnloadingObjects, value);
        }

        private bool showPotentialDisruptors;
        public bool ShowPotentialDisruptors {
            get => showPotentialDisruptors;
            set => Set(ref showPotentialDisruptors, value);
        }

        private double approachRateOverride = -1;
        public double ApproachRateOverride {
            get => approachRateOverride;
            set => Set(ref approachRateOverride, value);
        }

        private double overallDifficultyOverride = -1;
        public double OverallDifficultyOverride {
            get => overallDifficultyOverride;
            set => Set(ref overallDifficultyOverride, value);
        }

        private int physicsUpdateLeniency = 9;
        public int PhysicsUpdateLeniency {
            get => physicsUpdateLeniency;
            set => Set(ref physicsUpdateLeniency, value);
        }

        private bool getAutoFailFix;
        public bool GetAutoFailFix {
            get => getAutoFailFix;
            set => Set(ref getAutoFailFix, value);
        }

        private bool autoPlaceFix;
        public bool AutoPlaceFix {
            get => autoPlaceFix;
            set => Set(ref autoPlaceFix, value);
        }

        public AutoFailDetectorVm() {
            
        }
    }
}

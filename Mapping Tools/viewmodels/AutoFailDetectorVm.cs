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

        private bool _showUnloadingObjects = true;
        public bool ShowUnloadingObjects {
            get => _showUnloadingObjects;
            set => Set(ref _showUnloadingObjects, value);
        }

        private bool _showPotentialUnloadingObjects;
        public bool ShowPotentialUnloadingObjects {
            get => _showPotentialUnloadingObjects;
            set => Set(ref _showPotentialUnloadingObjects, value);
        }

        private bool _showPotentialDisruptors;
        public bool ShowPotentialDisruptors {
            get => _showPotentialDisruptors;
            set => Set(ref _showPotentialDisruptors, value);
        }

        private double _approachRateOverride = -1;
        public double ApproachRateOverride {
            get => _approachRateOverride;
            set => Set(ref _approachRateOverride, value);
        }

        private double _overallDifficultyOverride = -1;
        public double OverallDifficultyOverride {
            get => _overallDifficultyOverride;
            set => Set(ref _overallDifficultyOverride, value);
        }

        private int _physicsUpdateLeniency = 9;
        public int PhysicsUpdateLeniency {
            get => _physicsUpdateLeniency;
            set => Set(ref _physicsUpdateLeniency, value);
        }

        private bool _getAutoFailFix;
        public bool GetAutoFailFix {
            get => _getAutoFailFix;
            set => Set(ref _getAutoFailFix, value);
        }

        private bool _autoPlaceFix;
        public bool AutoPlaceFix {
            get => _autoPlaceFix;
            set => Set(ref _autoPlaceFix, value);
        }

        public AutoFailDetectorVm() {
            
        }
    }
}

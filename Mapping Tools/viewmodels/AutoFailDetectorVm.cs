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

        private bool _showAutoFailTimes = true;
        public bool ShowAutoFailTimes {
            get => _showAutoFailTimes;
            set => Set(ref _showAutoFailTimes, value);
        }

        private bool _showUnloadingObjects;
        public bool ShowUnloadingObjects {
            get => _showUnloadingObjects;
            set => Set(ref _showUnloadingObjects, value);
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

        public AutoFailDetectorVm() {
            
        }
    }
}

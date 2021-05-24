using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class SliderCompletionatorVm : BindableBase {
        #region Properties

        [JsonIgnore]
        public string[] Paths { get; set; }

        [JsonIgnore]
        public bool Quick { get; set; }

        private ImportMode _importModeSetting;
        public ImportMode ImportModeSetting {
            get => _importModeSetting;
            set {
                if (Set(ref _importModeSetting, value)) {
                    RaisePropertyChanged(nameof(TimeCodeBoxVisibility));
                }
            }
        }

        public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

        public Visibility TimeCodeBoxVisibility => ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;

        private string _timeCode;
        public string TimeCode {
            get => _timeCode;
            set => Set(ref _timeCode, value);
        }

        private double _temporalLength;
        public double TemporalLength {
            get => _temporalLength;
            set => Set(ref _temporalLength, value);
        }

        private double _spatialLength;
        public double SpatialLength {
            get => _spatialLength;
            set => Set(ref _spatialLength, value);
        }

        private bool _moveAnchors;
        public bool MoveAnchors {
            get => _moveAnchors;
            set => Set(ref _moveAnchors, value);
        }

        #endregion

        public SliderCompletionatorVm() {
            ImportModeSetting = ImportMode.Selected;
            TemporalLength = -1;
            SpatialLength = 1;
            MoveAnchors = false;
            TimeCode = string.Empty;
        }

        public enum ImportMode {
            Selected,
            Bookmarked,
            Time,
            Everything
        }
    }
}

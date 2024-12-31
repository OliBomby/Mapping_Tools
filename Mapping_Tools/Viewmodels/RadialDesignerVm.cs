using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class RadialDesignerVm : BindableBase {
        #region Properties

        [JsonIgnore]
        public string[] Paths {
            get; set;
        }

        [JsonIgnore]
        public bool Quick {
            get; set;
        }

        private int copies;
        public int Copies {
            get => copies;
            set => Set(ref copies, value);
        }

        private double distance;
        public double Distance {
            get => distance;
            set => Set(ref distance, value);
        }

        private double localRotation;
        public double LocalRotation {
            get => localRotation;
            set => Set(ref localRotation, value);
        }

        private double globalRotation;
        public double GlobalRotation {
            get => globalRotation;
            set => Set(ref globalRotation, value);
        }

        public enum ImportMode {
            Selected,
            Bookmarked,
            Time,
            Everything
        }

        private ImportMode importModeSetting;
        public ImportMode ImportModeSetting {
            get => importModeSetting;
            set {
                if (Set(ref importModeSetting, value)) {
                    RaisePropertyChanged(nameof(TimeCodeBoxVisibility));
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

        private string timeCode;
        public string TimeCode {
            get => timeCode;
            set => Set(ref timeCode, value);
        }

        [JsonIgnore]
        public Visibility TimeCodeBoxVisibility =>
            ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;

        public enum CenterMode {
            First,
            Average
        }

        private CenterMode centerModeSetting;
        public CenterMode CenterModeSetting {
            get => centerModeSetting;
            set => Set(ref centerModeSetting, value);
        }

        public IEnumerable<CenterMode> CenterModes => Enum.GetValues(typeof(CenterMode)).Cast<CenterMode>();

        #endregion

        public RadialDesignerVm() {
            Copies = 1;
            Distance = 0;
            LocalRotation = 0;
            GlobalRotation = 0;

            ImportModeSetting = ImportMode.Selected;
            TimeCode = "";
        }
    }
}

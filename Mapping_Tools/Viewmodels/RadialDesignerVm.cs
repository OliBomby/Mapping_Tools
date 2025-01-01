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

        private string importTimeCode;
        public string ImportTimeCode {
            get => importTimeCode;
            set => Set(ref importTimeCode, value);
        }

        private string exportTimeCode;
        public string ExportTimeCode {
            get => exportTimeCode;
            set => Set(ref exportTimeCode, value);
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
                    RaisePropertyChanged(nameof(ImportTimeCodeBoxVisibility));
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

        [JsonIgnore]
        public Visibility ImportTimeCodeBoxVisibility =>
            ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;

        public enum ExportMode {
            Auto,
            Current,
            Time,
        }

        private ExportMode exportModeSetting;
        public ExportMode ExportModeSetting {
            get => exportModeSetting;
            set {
                if (Set(ref exportModeSetting, value)) {
                    RaisePropertyChanged(nameof(ExportTimeCodeBoxVisibility));
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<ExportMode> ExportModes => Enum.GetValues(typeof(ExportMode)).Cast<ExportMode>();

        [JsonIgnore]
        public Visibility ExportTimeCodeBoxVisibility =>
            ExportModeSetting == ExportMode.Time ? Visibility.Visible : Visibility.Collapsed;

        #endregion

        public RadialDesignerVm() {
            Copies = 1;
            Distance = 0;
            LocalRotation = 0;
            GlobalRotation = 0;

            ImportModeSetting = ImportMode.Selected;
            ImportTimeCode = "";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Dialogs.OsuPatternImport {
    public class OsuPatternImportDialogViewModel : BindableBase
    {
        private string _name;
        public string Name {
            get => _name;
            set => Set(ref _name, value);
        }

        private ImportMode _importModeSetting;
        public ImportMode ImportModeSetting {
            get => _importModeSetting;
            set {
                if (Set(ref _importModeSetting, value)) {
                    TimeCodeBoxVisibility = ImportModeSetting == ImportMode.Time ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        public IEnumerable<ImportMode> ImportModes => Enum.GetValues(typeof(ImportMode)).Cast<ImportMode>();

        private Visibility _timeCodeBoxVisibility;
        public Visibility TimeCodeBoxVisibility {
            get => _timeCodeBoxVisibility;
            set => Set(ref _timeCodeBoxVisibility, value);
        }

        private string _timeCode;
        public string TimeCode {
            get => _timeCode;
            set => Set(ref _timeCode, value);
        }

        public enum ImportMode {
            Selected,
            Bookmarked,
            Time
        }
    }
}
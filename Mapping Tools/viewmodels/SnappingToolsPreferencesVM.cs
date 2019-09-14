using Mapping_Tools.Classes.SnappingTools;
using Mapping_Tools.Classes.SystemTools;
using System.ComponentModel;

namespace Mapping_Tools.Viewmodels {
    public class SnappingToolsPreferencesVM {
        private SnappingToolsPreferences snappingToolsPreferences;
        public SnappingToolsPreferences SnappingToolsPreferences {
            get { return snappingToolsPreferences; }
            set { snappingToolsPreferences = value; }
        }

        public SnappingToolsPreferencesVM() {
            if (SettingsManager.Settings.SnappingToolsPreferences != null) {
                snappingToolsPreferences = SettingsManager.Settings.SnappingToolsPreferences;
            } else {
                snappingToolsPreferences = new SnappingToolsPreferences();
            }
            snappingToolsPreferences.PropertyChanged += SnappingToolsPreferences_PropertyChanged;
        }

        private void SnappingToolsPreferences_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            SettingsManager.Settings.SnappingToolsPreferences = snappingToolsPreferences;
        }
    }
}

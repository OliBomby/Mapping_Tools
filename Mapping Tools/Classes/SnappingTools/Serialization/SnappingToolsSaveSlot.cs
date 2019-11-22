using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.SnappingTools.Serialization {
    public class SnappingToolsSaveSlot : BindableBase {
        private string name;
        private Hotkey projectHotkey;

        public string Name {
            get => name;
            set => Set(ref name, value);
        }

        public Hotkey ProjectHotkey {
            get => projectHotkey;
            set => Set(ref projectHotkey, value);
        }

        public CommandImplementation SaveCommand { get; }
        public CommandImplementation LoadCommand { get; }

        [JsonIgnore]
        [CanBeNull]
        public SnappingToolsProject ParentProject { get; set; }

        public SnappingToolsPreferences Preferences { get; set; }

        public SnappingToolsSaveSlot() {
            Preferences = new SnappingToolsPreferences();

            // SaveCommand takes the CurrentPreferences and copies it to this instance.
            SaveCommand = new CommandImplementation(o => ParentProject?.SaveToSlot(this));
            // LoadCommand takes this instance and copies it to ProjectWindow's CurrentPreferences.
            LoadCommand = new CommandImplementation(o => ParentProject?.LoadFromSlot(this));
        }
    }
}
using System;
using System.Windows.Input;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.SnappingTools.Serialization {
    public class SnappingToolsSaveSlot : BindableBase, IDisposable {
        private string _name;
        private Hotkey _projectHotkey;
        private readonly string _hotkeyHandle;

        public string Name {
            get => _name;
            set => Set(ref _name, value);
        }

        public Hotkey ProjectHotkey {
            get => _projectHotkey;
            set {
                if (_projectHotkey.Equals(value)) return;
                Set(ref _projectHotkey, value);
                MainWindow.AppWindow.ListenerManager.ChangeActiveHotkeyHotkey(_hotkeyHandle, ProjectHotkey);
            }
        }

        public CommandImplementation SaveCommand { get; }
        public CommandImplementation LoadCommand { get; }

        [JsonIgnore]
        [CanBeNull]
        public SnappingToolsProject ParentProject { get; set; }

        public SnappingToolsPreferences Preferences { get; set; }

        public SnappingToolsSaveSlot() {
            Preferences = new SnappingToolsPreferences();
            _projectHotkey = new Hotkey(Key.None, ModifierKeys.None);

            // Setup hotkey stuff
            _hotkeyHandle = GenerateActiveHotkeyHandle();
            MainWindow.AppWindow.ListenerManager.AddActiveHotkey(_hotkeyHandle, 
                new ActionHotkey(ProjectHotkey, () => {
                    if (System.Windows.Application.Current.Dispatcher != null)
                        System.Windows.Application.Current.Dispatcher.Invoke(() => ParentProject?.LoadFromSlot(this));
                }));

            // SaveCommand takes the CurrentPreferences and copies it to this instance.
            SaveCommand = new CommandImplementation(o => ParentProject?.SaveToSlot(this));
            // LoadCommand takes this instance and copies it to ProjectWindow's CurrentPreferences.
            LoadCommand = new CommandImplementation(o => ParentProject?.LoadFromSlot(this));
        }

        public object Clone() {
            return new SnappingToolsSaveSlot {Name = Name, ProjectHotkey = (Hotkey)ProjectHotkey?.Clone(), Preferences = (SnappingToolsPreferences)Preferences.Clone()};
        }

        public void Dispose() {
            MainWindow.AppWindow.ListenerManager.RemoveActiveHotkey(_hotkeyHandle);
        }

        private static string GenerateActiveHotkeyHandle() {
            return $"SaveSlot - {MainWindow.MainRandom.Next(int.MaxValue)}";
        }
    }
}
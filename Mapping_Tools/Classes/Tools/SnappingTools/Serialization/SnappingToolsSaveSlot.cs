using System;
using System.ComponentModel;
using System.Windows.Input;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.Tools.SnappingTools.Serialization;

public class SnappingToolsSaveSlot : BindableBase, IDisposable {
    private string name;
    private Hotkey projectHotkey;
    [JsonIgnore]
    private readonly string hotkeyHandle;

    public string Name {
        get => name;
        set => Set(ref name, value);
    }

    public Hotkey ProjectHotkey {
        get => projectHotkey;
        set => Set(ref projectHotkey, value);
    }

    [JsonIgnore]
    public CommandImplementation SaveCommand { get; }
    [JsonIgnore]
    public CommandImplementation LoadCommand { get; }

    [JsonIgnore]
    [CanBeNull]
    public SnappingToolsProject ParentProject { get; set; }

    public SnappingToolsPreferences Preferences { get; set; }

    public SnappingToolsSaveSlot() {
        Preferences = new SnappingToolsPreferences();

        // Setup hotkey stuff
        projectHotkey = new Hotkey(Key.None, ModifierKeys.None);
        hotkeyHandle = GenerateActiveHotkeyHandle();

        PropertyChanged += OnPropertyChanged;

        // SaveCommand takes the CurrentPreferences and copies it to this instance.
        SaveCommand = new CommandImplementation(o => ParentProject?.SaveToSlot(this));
        // LoadCommand takes this instance and copies it to ProjectWindow's CurrentPreferences.
        LoadCommand = new CommandImplementation(o => ParentProject?.LoadFromSlot(this));
    }

    public void RefreshHotkey() {
        MainWindow.AppWindow.ListenerManager.RemoveActiveHotkey(hotkeyHandle);
        RegisterHotkey();
    }

    private void RegisterHotkey() {
        MainWindow.AppWindow.ListenerManager.AddActiveHotkey(hotkeyHandle, 
            new ActionHotkey(ProjectHotkey, () => {
                if (System.Windows.Application.Current.Dispatcher != null)
                    System.Windows.Application.Current.Dispatcher.Invoke(() => ParentProject?.LoadFromSlot(this));
            }));
    }

    private void UnRegisterHotkey() {
        MainWindow.AppWindow.ListenerManager.RemoveActiveHotkey(hotkeyHandle);
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
        if (e.PropertyName != "ProjectHotkey") return;
            
        MainWindow.AppWindow.ListenerManager.ChangeActiveHotkeyHotkey(hotkeyHandle, ProjectHotkey);
    }

    public object Clone() {
        return new SnappingToolsSaveSlot {Name = Name, ProjectHotkey = (Hotkey)ProjectHotkey?.Clone(), Preferences = (SnappingToolsPreferences)Preferences.Clone()};
    }

    public void Dispose() {
        UnRegisterHotkey();
    }

    private static string GenerateActiveHotkeyHandle() {
        var number = RNG.Next();
        while (MainWindow.AppWindow.ListenerManager.ActiveHotkeys.ContainsKey($"SaveSlot - {number}")) {
            number = RNG.Next();
        }
        return $"SaveSlot - {number}";
    }

    public void Activate() {
        RegisterHotkey();
    }

    public void Deactivate() {
        UnRegisterHotkey();
    }
}
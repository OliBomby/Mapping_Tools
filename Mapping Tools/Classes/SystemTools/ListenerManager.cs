using Mapping_Tools.Classes.Tools;
using NonInvasiveKeyboardHookLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Mapping_Tools.Classes.SystemTools {
    public class ListenerManager {
        public readonly FileSystemWatcher FsWatcher = new FileSystemWatcher();
        public readonly KeyboardHookManager keyboardHookManager = new KeyboardHookManager();
        public Dictionary<string, ActionHotkey> ActiveHotkeys = new Dictionary<string, ActionHotkey>();
        
        public ListenerManager() {
            InitFsWatcher();

            LoadHotkeys();
            ReloadHotkeys();
            keyboardHookManager.Start();

            SettingsManager.Settings.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case "OverrideOsuSave":
                    FsWatcher.EnableRaisingEvents = SettingsManager.Settings.OverrideOsuSave;
                    break;
                case "SongsPath":
                    FsWatcher.Path = SettingsManager.GetSongsPath();
                    break;
                case "QuickRunHotkey":
                    ChangeActiveHotkeyHotkey("QuickRunHotkey", SettingsManager.Settings.QuickRunHotkey);
                    break;
                case "BetterSaveHotkey":
                    ChangeActiveHotkeyHotkey("BetterSaveHotkey", SettingsManager.Settings.BetterSaveHotkey);
                    break;
            }
        }

        private void LoadHotkeys() {
            AddActiveHotkey("QuickRunHotkey", new ActionHotkey(SettingsManager.Settings.QuickRunHotkey, QuickRunCurrentTool));
            AddActiveHotkey("BetterSaveHotkey", new ActionHotkey(SettingsManager.Settings.BetterSaveHotkey, QuickBetterSave));
        }

        public void AddActiveHotkey(string name, ActionHotkey actionHotkey) {
            ActiveHotkeys.Add(name, actionHotkey);
            ReloadHotkeys();
        }

        public bool ChangeActiveHotkeyHotkey(string name, Hotkey hotkey) {
            if (ActiveHotkeys.ContainsKey(name)) {
                ActiveHotkeys[name].Hotkey = hotkey;
                ReloadHotkeys();
                return true;
            } else {
                return false;
            }
        }
        
        public void ReloadHotkeys() {
            try {
                keyboardHookManager.UnregisterAll();

                foreach (ActionHotkey ah in ActiveHotkeys.Values) {
                    RegisterHotkey(ah.Hotkey, ah.Action);
                }
            } catch { MessageBox.Show("Could not reload hotkeys.", "Warning"); }
        }

        private void RegisterHotkey(Hotkey hotkey, Action action) {
            if (hotkey != null)
                keyboardHookManager.RegisterHotkey(WindowsModifiersToOtherModifiers(hotkey.Modifiers), ResolveKey(hotkey.Key), action);
            //Console.WriteLine($"Registered hotkey {hotkey.Modifiers}, {hotkey.Key}, {action}");
        }

        static public int ResolveKey(System.Windows.Input.Key key) {
            return System.Windows.Input.KeyInterop.VirtualKeyFromKey(key);
        }

        private ModifierKeys[] WindowsModifiersToOtherModifiers(System.Windows.Input.ModifierKeys modifierKeys) {
            List<ModifierKeys> otherModifiers = new List<ModifierKeys>();

            if ((modifierKeys & System.Windows.Input.ModifierKeys.Alt) > 0)
                otherModifiers.Add(ModifierKeys.Alt);
            if ((modifierKeys & System.Windows.Input.ModifierKeys.Control) > 0)
                otherModifiers.Add(ModifierKeys.Control);
            if ((modifierKeys & System.Windows.Input.ModifierKeys.Shift) > 0)
                otherModifiers.Add(ModifierKeys.Shift);
            if ((modifierKeys & System.Windows.Input.ModifierKeys.Windows) > 0)
                otherModifiers.Add(ModifierKeys.WindowsKey);

            return otherModifiers.ToArray();
        }

        private void QuickBetterSave() {
            EditorReaderStuff.CoolSave();
        }

        private void QuickRunCurrentTool() {
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                if (MainWindow.AppWindow.GetCurrentView() is IQuickRun tool) {
                    tool.RunFinished -= Reload;
                    tool.RunFinished += Reload;
                    tool.QuickRun();
                }
            });
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void Reload(object sender, EventArgs e) {
            if (((RunToolCompletedEventArgs)e).NeedReload && SettingsManager.Settings.AutoReload) {
                var proc = Process.GetProcessesByName("osu!").FirstOrDefault();
                ;
                if (proc != null) {
                    var oldHandle = GetForegroundWindow();
                    if (oldHandle != proc.MainWindowHandle) {
                        SetForegroundWindow(proc.MainWindowHandle);
                        Thread.Sleep(300);
                    }
                }
                SendKeys.SendWait("^{L 10}");
                Thread.Sleep(100);
                SendKeys.SendWait("{ENTER}");
            }
        }

        private void InitFsWatcher() {
            try {
                FsWatcher.Path = SettingsManager.GetSongsPath();
            } catch { }

            FsWatcher.Filter = "*.osu";
            FsWatcher.Changed += OnChangedFsWatcher;
            FsWatcher.EnableRaisingEvents = SettingsManager.Settings.OverrideOsuSave;
            FsWatcher.IncludeSubdirectories = true;
        }

        private static void OnChangedFsWatcher(object sender, FileSystemEventArgs e) {
            var currentPath = IOHelper.GetCurrentBeatmap();

            if (e.FullPath != currentPath) {
                return;
            }

            var proc = Process.GetProcessesByName("osu!").FirstOrDefault();
            if (proc != null) {
                var oldHandle = GetForegroundWindow();
                if (oldHandle != proc.MainWindowHandle) {
                    return;
                }
            }

            string hashString = "";
            try {
                if (File.Exists(currentPath)) {
                    hashString = EditorReaderStuff.GetMD5FromPath(currentPath);
                }
            }
            catch {
                return;
            }

            if (EditorReaderStuff.DontCoolSaveWhenMD5EqualsThisString == hashString) {
                return;
            }

            EditorReaderStuff.CoolSave();
        }
    }
}

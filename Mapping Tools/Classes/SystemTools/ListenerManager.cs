using Editor_Reader;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using NonInvasiveKeyboardHookLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.ToolHelpers;
using ModifierKeys = NonInvasiveKeyboardHookLibrary.ModifierKeys;

namespace Mapping_Tools.Classes.SystemTools {
    public class ListenerManager {
        private string previousPeriodicBackupHash;

        public readonly FileSystemWatcher FsWatcher = new FileSystemWatcher();
        public readonly KeyboardHookManager KeyboardHookManager = new KeyboardHookManager();
        public Dictionary<string, ActionHotkey> ActiveHotkeys = new Dictionary<string, ActionHotkey>();
        public DispatcherTimer PeriodicBackupTimer;

        public ListenerManager()
        {
            InitFsWatcher();

            LoadHotkeys();
            ReloadHotkeys();
            KeyboardHookManager.Start();

            InitPeriodicBackupTimer();

            SettingsManager.Settings.PropertyChanged += OnSettingsChanged;
        }

        private void InitPeriodicBackupTimer() {
            previousPeriodicBackupHash = string.Empty;

            PeriodicBackupTimer = new DispatcherTimer(DispatcherPriority.Background)
                {Interval = SettingsManager.Settings.PeriodicBackupInterval};
            PeriodicBackupTimer.Tick += PeriodicBackupTimerOnTick;

            if (SettingsManager.Settings.MakePeriodicBackups) {
                PeriodicBackupTimer.Start();
            }
        }

        private void PeriodicBackupTimerOnTick(object sender, EventArgs e) {
            try {
                // Get the newest beatmap, save a temp version, get the hash and compare it to the previous hash, backup temp file
                var path = IOHelper.GetCurrentBeatmap();

                if (string.IsNullOrEmpty(path)) return;

                // Don't make period backup if the editor is not open
                if (!EditorReaderStuff.IsEditorOpen()) return;

                EditorReader reader = null;
                try {
                    reader = EditorReaderStuff.GetFullEditorReader();
                } catch (Exception ex) {
                    Console.WriteLine(ex.MessageStackTrace());
                }

                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);

                // Save temp version
                var tempPath = Path.Combine(MainWindow.AppDataPath, "temp.osu");

                Editor.SaveFile(tempPath, editor.Beatmap.GetLines());

                // Get MD5 from temp file
                var currentMapHash = EditorReaderStuff.GetMD5FromPath(tempPath);

                // Comparing with previously made periodic backup
                if (currentMapHash == previousPeriodicBackupHash) {
                    return;
                }

                // Saving backup of the map
                BackupManager.SaveMapBackup(tempPath, true, Path.GetFileName(path), "PB");  // PB stands for Periodic Backup

                previousPeriodicBackupHash = currentMapHash;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "OverrideOsuSave":
                    FsWatcher.EnableRaisingEvents = SettingsManager.Settings.OverrideOsuSave;
                    break;
                case "SongsPath":
                    try
                    {
                        FsWatcher.Path = SettingsManager.GetSongsPath();
                    }
                    catch
                    {
                        // ignored
                    }

                    break;
                case "QuickRunHotkey":
                    ChangeActiveHotkeyHotkey("QuickRunHotkey", SettingsManager.Settings.QuickRunHotkey);
                    break;
                case "QuickUndoHotkey":
                    ChangeActiveHotkeyHotkey("QuickUndoHotkey", SettingsManager.Settings.QuickUndoHotkey);
                    break;
                case "BetterSaveHotkey":
                    ChangeActiveHotkeyHotkey("BetterSaveHotkey", SettingsManager.Settings.BetterSaveHotkey);
                    break;
                case "MakePeriodicBackups":
                    if (SettingsManager.Settings.MakePeriodicBackups) {
                        PeriodicBackupTimer.Start();
                    } else {
                        PeriodicBackupTimer.Stop();
                    }
                    break;
                case "PeriodicBackupInterval":
                    PeriodicBackupTimer.Interval = SettingsManager.Settings.PeriodicBackupInterval;
                    break;
            }
        }

        private void LoadHotkeys()
        {
            AddActiveHotkey("QuickRunHotkey", new ActionHotkey(SettingsManager.Settings.QuickRunHotkey, SmartQuickRun));
            AddActiveHotkey("BetterSaveHotkey", new ActionHotkey(SettingsManager.Settings.BetterSaveHotkey, QuickBetterSave));
            AddActiveHotkey("QuickUndoHotkey", new ActionHotkey(SettingsManager.Settings.QuickUndoHotkey, QuickUndo));
        }

        public void AddActiveHotkey(string name, ActionHotkey actionHotkey)
        {
            if (ActiveHotkeys.ContainsKey(name))
            {
                return;
            }

            ActiveHotkeys.Add(name, actionHotkey);
            ReloadHotkeys();
        }

        public void RemoveActiveHotkey(string name)
        {
            if (!ActiveHotkeys.ContainsKey(name))
            {
                return;
            }

            ActiveHotkeys.Remove(name);
            ReloadHotkeys();
        }

        public bool ChangeActiveHotkeyHotkey(string name, Hotkey hotkey)
        {
            if (ActiveHotkeys.ContainsKey(name))
            {
                ActiveHotkeys[name].Hotkey = hotkey;
                ReloadHotkeys();
                return true;
            }

            return false;
        }

        public void ReloadHotkeys()
        {
            try {
                KeyboardHookManager.UnregisterAll();

                foreach (var ah in ActiveHotkeys.Values.Where(ah =>
                    ah.Hotkey != null && ah.Action != null && ah.Hotkey.Key != Key.None))
                {
                    RegisterHotkey(ah.Hotkey, ah.Action);
                }
            }
            catch (Exception ex) {
                ex.Show();
            }
        }

        private void RegisterHotkey(Hotkey hotkey, Action action)
        {
            if (hotkey != null)
                KeyboardHookManager.RegisterHotkey(WindowsModifiersToOtherModifiers(hotkey.Modifiers), ResolveKey(hotkey.Key), action);
            //Console.WriteLine($"Registered hotkey {hotkey.Modifiers}, {hotkey.Key}, {action}");
        }

        public static int ResolveKey(Key key)
        {
            return KeyInterop.VirtualKeyFromKey(key);
        }

        private ModifierKeys[] WindowsModifiersToOtherModifiers(System.Windows.Input.ModifierKeys modifierKeys)
        {
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

        private static void QuickUndo() {
            System.Windows.Application.Current.Dispatcher.Invoke(BackupManager.QuickUndo);
        }

        private static void QuickBetterSave() {
            try {
                EditorReaderStuff.BetterSave();
            }
            catch (Exception e) {
                e.Show();
            }
        }

        private static void SmartQuickRun()
        {
            try
            {
                if (!SettingsManager.Settings.SmartQuickRunEnabled) { QuickRunCurrentTool(); return; }

                var reader = EditorReaderStuff.GetFullEditorReader();

                var so = reader.hitObjects.Count(o => o.IsSelected);
                IQuickRun tool = null;

                if (System.Windows.Application.Current.Dispatcher == null) return;

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    if (so == 0)
                    {
                        if (SettingsManager.Settings.NoneQuickRunTool == "<Current Tool>") { QuickRunCurrentTool(); return; }
                        if (!(MainWindow.AppWindow.Views.GetView(SettingsManager.Settings.NoneQuickRunTool) is IQuickRun noneTool)) return;

                        tool = noneTool;
                    }
                    else if (so == 1)
                    {
                        if (SettingsManager.Settings.SingleQuickRunTool == "<Current Tool>") { QuickRunCurrentTool(); return; }
                        if (!(MainWindow.AppWindow.Views.GetView(SettingsManager.Settings.SingleQuickRunTool) is IQuickRun singleTool)) return;

                        tool = singleTool;
                    }
                    else if (so > 1)
                    {
                        if (SettingsManager.Settings.MultipleQuickRunTool == "<Current Tool>") { QuickRunCurrentTool(); return; }
                        if (!(MainWindow.AppWindow.Views.GetView(SettingsManager.Settings.MultipleQuickRunTool) is IQuickRun multiTool)) return;

                        tool = multiTool;
                    }

                    if (tool == null) return;

                    tool.QuickRun();
                });
            }
            catch (Exception ex)
            {
                ex.Show();
            }
        }

        private static void QuickRunCurrentTool()
        {
            try
            {
                if (System.Windows.Application.Current.Dispatcher == null) return;

                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    if (!(MainWindow.AppWindow.GetCurrentView() is IQuickRun tool)) return;
                    tool.QuickRun();
                });
            }
            catch (Exception ex)
            {
                ex.Show();
            }
        }

        public static void RunFinishedEventHandler(object sender, EventArgs e) {
            if (!((RunToolCompletedEventArgs)e).Quick ||
                !((RunToolCompletedEventArgs)e).Success || 
                !((RunToolCompletedEventArgs)e).NeedReload || 
                !SettingsManager.Settings.AutoReload) return;

            ForceReloadEditor();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void ForceReloadEditor() {
            var proc = System.Diagnostics.Process.GetProcessesByName("osu!").FirstOrDefault();

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

        private void InitFsWatcher()
        {
            try
            {
                FsWatcher.Path = SettingsManager.GetSongsPath();
            }
            catch
            {
                // ignored
            }

            FsWatcher.Filter = "*.osu";
            FsWatcher.Changed += OnChangedFsWatcher;
            FsWatcher.EnableRaisingEvents = SettingsManager.Settings.OverrideOsuSave;
            FsWatcher.IncludeSubdirectories = true;
        }

        private static void OnChangedFsWatcher(object sender, FileSystemEventArgs e)
        {
            try {
                var currentPath = IOHelper.GetCurrentBeatmap();

                if (e.FullPath != currentPath) {
                    return;
                }

                var proc = System.Diagnostics.Process.GetProcessesByName("osu!").FirstOrDefault();
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

                EditorReaderStuff.BetterSave();
            }
            catch (Exception ex) {
                MessageBox.Show("Failed to overwrite osu! save with BetterSave.");
                ex.Show();
            }
        }
    }
}

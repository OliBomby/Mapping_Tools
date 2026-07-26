using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.ApplicationServices.Workspace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Classes.SystemTools {
    internal static class LegacySettingsMapper {
        public static ApplicationSettings ToApplication(Settings source) {
            return new ApplicationSettings {
                RecentMaps = source.RecentMaps
                    .Select(paths => new RecentBeatmap(paths[0], paths[1]))
                    .ToList(),
                FavoriteTools = new List<string>(source.FavoriteTools),
                MainWindowRestoreBounds = source.MainWindowRestoreBounds is { } bounds
                    ? new WindowBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height)
                    : null,
                MainWindowMaximized = source.MainWindowMaximized,
                OsuPath = source.OsuPath,
                SongsPath = source.SongsPath,
                BackupsPath = source.BackupsPath,
                OsuConfigPath = source.OsuConfigPath,
                MakeBackups = source.MakeBackups,
                UseEditorReader = source.UseEditorReader,
                OverrideOsuSave = source.OverrideOsuSave,
                AutoReload = source.AutoReload,
                AlwaysQuickRun = source.AlwaysQuickRun,
                QuickRunHotkey = ToApplication(source.QuickRunHotkey),
                SmartQuickRunEnabled = source.SmartQuickRunEnabled,
                NoneQuickRunTool = source.NoneQuickRunTool,
                SingleQuickRunTool = source.SingleQuickRunTool,
                MultipleQuickRunTool = source.MultipleQuickRunTool,
                BetterSaveHotkey = ToApplication(source.BetterSaveHotkey),
                MaxBackupFiles = source.MaxBackupFiles,
                MakePeriodicBackups = source.MakePeriodicBackups,
                PeriodicBackupInterval = source.PeriodicBackupInterval,
                CurrentBeatmapDefaultFolder = source.CurrentBeatmapDefaultFolder,
                Theme = source.Theme,
                QuickUndoHotkey = ToApplication(source.QuickUndoHotkey),
                SkipVersion = source.SkipVersion?.ToString()
            };
        }

        public static void Apply(ApplicationSettings source, Settings destination) {
            destination.RecentMaps = source.RecentMaps
                .Select(recent => new[] { recent.Path, recent.DisplayDate })
                .ToList();
            destination.FavoriteTools = new List<string>(source.FavoriteTools);
            destination.MainWindowRestoreBounds = source.MainWindowRestoreBounds is { } bounds
                ? new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height)
                : null;
            destination.MainWindowMaximized = source.MainWindowMaximized;
            destination.OsuPath = source.OsuPath;
            destination.SongsPath = source.SongsPath;
            destination.BackupsPath = source.BackupsPath;
            destination.OsuConfigPath = source.OsuConfigPath;
            destination.MakeBackups = source.MakeBackups;
            destination.UseEditorReader = source.UseEditorReader;
            destination.OverrideOsuSave = source.OverrideOsuSave;
            destination.AutoReload = source.AutoReload;
            destination.AlwaysQuickRun = source.AlwaysQuickRun;
            destination.QuickRunHotkey = ToLegacy(source.QuickRunHotkey);
            destination.SmartQuickRunEnabled = source.SmartQuickRunEnabled;
            destination.NoneQuickRunTool = source.NoneQuickRunTool;
            destination.SingleQuickRunTool = source.SingleQuickRunTool;
            destination.MultipleQuickRunTool = source.MultipleQuickRunTool;
            destination.BetterSaveHotkey = ToLegacy(source.BetterSaveHotkey);
            destination.MaxBackupFiles = source.MaxBackupFiles;
            destination.MakePeriodicBackups = source.MakePeriodicBackups;
            destination.PeriodicBackupInterval = source.PeriodicBackupInterval;
            destination.CurrentBeatmapDefaultFolder = source.CurrentBeatmapDefaultFolder;
            destination.Theme = source.Theme;
            destination.QuickUndoHotkey = ToLegacy(source.QuickUndoHotkey);
            destination.SkipVersion = Version.TryParse(source.SkipVersion, out Version version)
                ? version
                : null;
        }

        private static HotkeySettings ToApplication(Hotkey hotkey) {
            return hotkey is null
                ? null
                : new HotkeySettings((int)hotkey.Key, (int)hotkey.Modifiers);
        }

        private static Hotkey ToLegacy(HotkeySettings hotkey) {
            return hotkey is null
                ? null
                : new Hotkey((Key)hotkey.Key, (ModifierKeys)hotkey.Modifiers);
        }
    }
}

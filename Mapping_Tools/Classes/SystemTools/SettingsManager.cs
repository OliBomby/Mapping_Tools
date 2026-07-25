using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools {
    public static class SettingsManager {
        private static IApplicationDirectories directories;
        private static ISettingsService settingsService;
        private static ISettingsPathService settingsPaths;

        public static readonly Settings Settings = new();
        public static bool InstanceComplete;

        public static string ApplicationDataPath => Directories.ApplicationData;
        public static string ExportPath => Directories.Exports;
        public static string ConfigurationFile => Directories.ConfigurationFile;

        private static IApplicationDirectories Directories {
            get {
                EnsureConfigured();
                return directories;
            }
        }

        public static void Configure(IApplicationDirectories applicationDirectories) {
            directories = applicationDirectories ??
                throw new ArgumentNullException(nameof(applicationDirectories));
            directories.EnsureCreated();

            ISettingsPathEnvironment environment = new WindowsSettingsPathEnvironment();
            settingsPaths = new SettingsPathService(directories, environment);
            settingsService = new SettingsService(
                new JsonSettingsStore(directories),
                settingsPaths);
        }

        public static void LoadConfig() {
            EnsureConfigured();
            bool usedFallbackOsuPath = false;
            try {
                SettingsLoadResult result = settingsService.LoadOrCreate();
                LegacySettingsMapper.Apply(result.Settings, Settings);
                usedFallbackOsuPath = result.UsedFallbackOsuPath;
                InstanceComplete = true;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("User-specific configuration could not be loaded!");
                ex.Show();
                InstanceComplete = false;

                try {
                    usedFallbackOsuPath = ApplyDefaultPaths();
                }
                catch (Exception pathException) {
                    pathException.Show();
                }
            }

            if (usedFallbackOsuPath) {
                MessageBox.Show(
                    "Could not automatically find osu! install directory. " +
                    "Please set the correct paths in the Preferences.");
            }
        }

        public static bool WriteToJson(bool doLoading=false) {
            EnsureConfigured();
            try {
                settingsService.Save(LegacySettingsMapper.ToApplication(Settings));
                if (doLoading) {
                    SettingsLoadResult result = settingsService.LoadOrCreate();
                    LegacySettingsMapper.Apply(result.Settings, Settings);
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("User-specific configuration could not be saved!");
                ex.Show();
                return false;
            }

            return true;
        }

        public static void AddRecentMap(string[] paths, DateTime date) {
            foreach (var path in paths)
            {
                Settings.RecentMaps.RemoveAll(o => o[0] == path);
                if (Settings.RecentMaps.Count > 19) {
                    try {
                        Settings.RecentMaps.Remove(Settings.RecentMaps.Last());
                    } catch (ArgumentOutOfRangeException) {
                    }
                }
                Settings.RecentMaps.Insert(0, new[] { path, date.ToString(CultureInfo.CurrentCulture) });
            }
        }

        public static void DefaultPaths() {
            EnsureConfigured();
            if (ApplyDefaultPaths()) {
                MessageBox.Show(
                    "Could not automatically find osu! install directory. " +
                    "Please set the correct paths in the Preferences.");
            }
        }

        public static List<string[]> GetRecentMaps() {
            return Settings.RecentMaps;
        }

        public static string[] GetLatestCurrentMaps() {
            if (GetRecentMaps().Count > 0) {
                return GetRecentMaps()[0][0].Split('|');
            } else {
                return new[] { "" };
            }
        }

        public static string GetOsuPath() {
            return Settings.OsuPath;
        }

        public static string GetSongsPath() {
            return Settings.SongsPath;
        }

        public static string GetBackupsPath() {
            return Settings.BackupsPath;
        }

        public static bool GetMakeBackups() {
            return Settings.MakeBackups;
        }

        internal static void UpdateSettings() {
            Settings.MainWindowMaximized = MainWindow.AppWindow.WindowState == WindowState.Maximized;
            if (MainWindow.AppWindow.WindowState == WindowState.Maximized) {
                Settings.MainWindowRestoreBounds = MainWindow.AppWindow.RestoreBounds;
            } else{
                Settings.MainWindowRestoreBounds = new Rect(new Point(
                    MainWindow.AppWindow.Left,
                    MainWindow.AppWindow.Top
                    ), new Vector(
                    MainWindow.AppWindow.Width,
                    MainWindow.AppWindow.Height));
            }
        }

        private static bool ApplyDefaultPaths() {
            ApplicationSettings applicationSettings =
                LegacySettingsMapper.ToApplication(Settings);
            SettingsPathResult result = settingsPaths.ApplyDefaults(applicationSettings);
            LegacySettingsMapper.Apply(applicationSettings, Settings);
            return result.UsedFallbackOsuPath;
        }

        private static void EnsureConfigured() {
            if (directories is null) {
                Configure(new ApplicationDirectories());
            }
        }
    }
}

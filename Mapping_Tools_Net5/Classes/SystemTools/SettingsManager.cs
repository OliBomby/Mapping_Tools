using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools {
    public static class SettingsManager {
        private static string JsonPath { get; set; }
        private static readonly JsonSerializer Serializer = new JsonSerializer {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public static readonly Settings Settings = new Settings();
        public static bool InstanceComplete;

        public static void LoadConfig() {
            JsonPath = Path.Combine(MainWindow.AppDataPath, "config.json");
            InstanceComplete = File.Exists(JsonPath) ? LoadFromJson() : CreateJson();
            DefaultPaths();
        }

        private static bool LoadFromJson() {
            try {
                using( StreamReader sr = new StreamReader(JsonPath)) {
                    using (JsonReader reader = new JsonTextReader(sr)) {
                        Settings newSettings = Serializer.Deserialize<Settings>(reader);
                        newSettings.CopyTo(Settings);
                    }
                }
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("User-specific configuration could not be loaded!");
                ex.Show();
                return false;
            }
            return true;
        }

        private static bool CreateJson() {
            try {
                using( StreamWriter sw = new StreamWriter(JsonPath)) {
                    using (JsonWriter writer = new JsonTextWriter(sw)) {
                        Serializer.Serialize(writer, Settings);
                    }
                }
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("User-specific configuration could not be loaded!");
                ex.Show();
                return false;
            }
            return true;
        }

        public static bool WriteToJson(bool doLoading=false) {
            try {
                using( StreamWriter sw = new StreamWriter(JsonPath)) {
                    using (JsonWriter writer = new JsonTextWriter(sw)) {
                        Serializer.Serialize(writer, Settings);
                    }
                }
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("User-specific configuration could not be saved!");
                ex.Show();
                return false;
            }

            if( doLoading ) {
                LoadFromJson();
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
            if (Settings.OsuPath == "") {
                try {
                    var regKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                    Settings.OsuPath = FindByDisplayName(regKey, "osu!");
                } catch (Exception) {
                    Settings.OsuPath = Path.Combine(MainWindow.AppCommon, "osu!");
                }
            }

            if (Settings.SongsPath == "") {
                Settings.SongsPath = Path.Combine(Settings.OsuPath, "Songs");
            }

            if (Settings.BackupsPath == "") {
                Settings.BackupsPath = Path.Combine(MainWindow.AppDataPath, "Backups");
                Directory.CreateDirectory(Settings.BackupsPath);
            }
        }

        private static string FindByDisplayName(RegistryKey parentKey, string name) {
            var nameList = parentKey.GetSubKeyNames();
            foreach (var t in nameList)
            {
                RegistryKey regKey = parentKey.OpenSubKey(t);
                try {
                    if (regKey != null && regKey.GetValue("DisplayName").ToString() == name) {
                        return Path.GetDirectoryName(regKey.GetValue("UninstallString").ToString());
                    }
                } catch (NullReferenceException) { }
            }

            throw new Exception();
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
    }
}

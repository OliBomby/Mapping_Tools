using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools {
    public static class SettingsManager {
        private static readonly string JSONPath = Path.Combine(MainWindow.AppDataPath, "config.json");
        private static readonly JsonSerializer Serializer = new JsonSerializer {
            NullValueHandling = NullValueHandling.Ignore
        };

        public static readonly Settings Settings = new Settings();

        public static void LoadConfig() {
            bool instanceComplete = File.Exists(JSONPath) ? LoadFromJSON() : CreateJSON();
            DefaultPaths();
        }

        private static bool LoadFromJSON() {
            try {
                using( StreamReader sr = new StreamReader(JSONPath) )
                using( JsonReader reader = new JsonTextReader(sr) ) {
                    Settings newSettings = Serializer.Deserialize<Settings>(reader);
                    newSettings.CopyTo(Settings);
                }
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("User-specific configuration could not be loaded!");
                return false;
            }
            return true;
        }

        private static bool CreateJSON() {
            try {
                using( StreamWriter sw = new StreamWriter(JSONPath) )
                using( JsonWriter writer = new JsonTextWriter(sw) ) {
                    Serializer.Serialize(writer, Settings);
                }
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("User-specific configuration could not be loaded!");
                return false;
            }
            return true;
        }

        public static bool WriteToJSON(bool doLoading=false) {
            try {
                using( StreamWriter sw = new StreamWriter(JSONPath) )
                using( JsonWriter writer = new JsonTextWriter(sw) ) {
                    Serializer.Serialize(writer, Settings);
                }
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("User-specific configuration could not be saved!");
                return false;
            }

            if( doLoading ) {
                LoadFromJSON();
            }

            return true;
        }

        public static void AddRecentMap(string path, DateTime date) {
            Settings.AddRecentMaps(path, date);
        }

        public static void DefaultPaths() {
            if (Settings.OsuPath == "") {
                RegistryKey regKey;
                try {
                    regKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                } catch (Exception) {
                    regKey = null;
                }
                if (regKey != null)
                    Settings.OsuPath = FindByDisplayName(regKey, "osu!");
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
            string[] nameList = parentKey.GetSubKeyNames();
            for (int i = 0; i < nameList.Length; i++) {
                RegistryKey regKey = parentKey.OpenSubKey(nameList[i]);
                try {
                    if (regKey.GetValue("DisplayName").ToString() == name) {
                        return Path.GetDirectoryName(regKey.GetValue("UninstallString").ToString());
                    }
                } catch (NullReferenceException) { }
            }
            return "";
        }

        public static List<string[]> GetRecentMaps() {
            return Settings.RecentMaps;
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
    }
}

using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools {
    public class SettingsManager {
        private static readonly string JSONPath = Path.Combine(MainWindow.AppWindow.AppDataPath, "config.json");
        private static readonly JsonSerializer Serializer = new JsonSerializer {
            NullValueHandling = NullValueHandling.Ignore
        };

        private Settings settings;

        public SettingsManager() {
            bool instanceComplete = File.Exists(JSONPath) ? LoadFromJSON() : CreateJSON();
            DefaultPaths();
        }

        private bool LoadFromJSON() {
            try {
                using( StreamReader sr = new StreamReader(JSONPath) )
                using( JsonReader reader = new JsonTextReader(sr) ) {
                    settings = Serializer.Deserialize<Settings>(reader);
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

        private bool CreateJSON() {
            try {
                settings = new Settings();

                using( StreamWriter sw = new StreamWriter(JSONPath) )
                using( JsonWriter writer = new JsonTextWriter(sw) ) {
                    Serializer.Serialize(writer, settings);
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

        public bool WriteToJSON(bool doLoading) {
            try {
                using( StreamWriter sw = new StreamWriter(JSONPath) )
                using( JsonWriter writer = new JsonTextWriter(sw) ) {
                    Serializer.Serialize(writer, settings);
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
        public bool AddRecentMaps(String path, DateTime date, bool doLoading) {
            settings.AddRecentMaps(path, date);

            if( WriteToJSON(doLoading) )
                return true;
            else
                return false;
        }

        public void DefaultPaths() {
            if (settings.OsuPath == "") {
                RegistryKey regKey;
                try {
                    regKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                } catch (Exception) {
                    regKey = null;
                }
                if (regKey != null)
                    settings.OsuPath = FindByDisplayName(regKey, "osu!");
            }

            if (settings.SongsPath == "") {
                settings.SongsPath = Path.Combine(settings.OsuPath, "Songs");
            }
        }

        private string FindByDisplayName(RegistryKey parentKey, string name) {
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

        public List<string[]> GetRecentMaps() {
            return settings.RecentMaps;
        }

        public string GetOsuPath() {
            return settings.OsuPath;
        }

        public string GetSongsPath() {
            return settings.SongsPath;
        }
    }
}

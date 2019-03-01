using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools {
    public class SettingsManager {
        private static readonly string Path = Environment.CurrentDirectory + "\\config.json";
        private static readonly JsonSerializer Serializer = new JsonSerializer {
            NullValueHandling = NullValueHandling.Ignore
        };

        private Settings settings;

        public SettingsManager() {
            bool instanceComplete = File.Exists(Path) ? LoadFromJSON() : CreateJSON();
        }

        private bool LoadFromJSON() {
            try {
                using( StreamReader sr = new StreamReader(Path) )
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

                using( StreamWriter sw = new StreamWriter(Path) )
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
                using( StreamWriter sw = new StreamWriter(Path) )
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

        public List<string[]> GetRecentMaps() {
            return settings.RecentMaps;
        }
    }
}

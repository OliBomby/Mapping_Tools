using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools {
    public class SettingsManager {
        private static readonly string Path = Environment.CurrentDirectory + "\\config.json";
        private static JsonSerializer Serializer { get; set; }

        public Settings settings;

        public SettingsManager() {
            Serializer = new JsonSerializer {
                NullValueHandling = NullValueHandling.Ignore
            };

            if( File.Exists(Path) ) {
                try {
                    LoadFromJSON();
                }
                catch( Exception ex ) {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);

                    MessageBox.Show("User-specific configuration could not be loaded!");
                }
            }

            else {
                try {
                    CreateJSON();
                }
                catch( Exception ex ) {
                    MessageBox.Show("User-specific configuration could not be loaded!");
                }
            }

        }

        void LoadFromJSON() {
            using( StreamReader sr = new StreamReader(Path) )
            using( JsonReader reader = new JsonTextReader(sr) ) {
                settings = Serializer.Deserialize<Settings>(reader);
            }
        }

        void CreateJSON() {
            settings = new Settings();

            using( StreamWriter sw = new StreamWriter(Path) )
            using( JsonWriter writer = new JsonTextWriter(sw) ) {
                Serializer.Serialize(writer, settings);
            }
        }

        public void WriteToJSON() {
            try {
                using( StreamWriter sw = new StreamWriter(Path) )
                using( JsonWriter writer = new JsonTextWriter(sw) ) {
                    Serializer.Serialize(writer, settings);
                }
            }
            catch( Exception ex ) {
                MessageBox.Show("User-specific configuration could not be saved!");
            }
        }
    }
}

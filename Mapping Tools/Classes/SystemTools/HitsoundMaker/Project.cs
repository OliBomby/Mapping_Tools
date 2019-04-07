using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.ViewSettings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SystemTools.HitsoundMaker {
    public class Project {
        public string Name;
        public HitsoundMakerSettings Settings { get; set; }

        public Project(string name) {
            Name = name;
            Settings = new HitsoundMakerSettings();
        }

        public Project(string name, HitsoundMakerSettings settings) {
            Name = name;
            Settings = settings;
        }

        public string GetSamplePath() {
            return Path.Combine(GetProjectPath(), "Samples");
        }

        public string GetExportPath() {
            return Path.Combine(GetProjectPath(), "Export");
        }

        public string GetJSONPath() {
            return Path.Combine(GetProjectPath(), "config.json");
        }

        public string GetProjectPath() {
            return Path.Combine(MainWindow.AppWindow.HSProjectPath, Name);
        }
    }
}

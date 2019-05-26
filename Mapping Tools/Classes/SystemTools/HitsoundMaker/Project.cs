using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Viewmodels;
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
        public HitsoundMakerVM Settings { get; set; }

        public Project(string name) {
            Name = name;
            Settings = new HitsoundMakerVM();
        }

        public Project(string name, HitsoundMakerVM settings) {
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

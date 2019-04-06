using Mapping_Tools.Classes.HitsoundStuff;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SystemTools.HitsoundMaker {
    public class Project {
        public String ProjectPath;
        public String SamplePath;
        public String ExportPath;
        public String JSONPath;
        public string BaseBeatmap { get; set; }
        public Sample DefaultSample { get; set; }
        public ObservableCollection<HitsoundLayer> HitsoundLayers { get; set; }

        public Project(String projectpath) {
            BaseBeatmap = "";
            DefaultSample = new Sample(0, 0, "", int.MaxValue);
            HitsoundLayers = new ObservableCollection<HitsoundLayer>();
            SetDefaultPaths(projectpath);
        }

        private void SetDefaultPaths(String projectpath) {
            ProjectPath = projectpath;
            SamplePath = Path.Combine(ProjectPath, "Samples");
            ExportPath = Path.Combine(ProjectPath, "Exports");
            JSONPath = Path.Combine(ProjectPath, "config.json");
        }

        public Project(string baseBeatmap, Sample defaultSample, ObservableCollection<HitsoundLayer> hitsoundLayers, String projectpath) {
            BaseBeatmap = baseBeatmap;
            DefaultSample = defaultSample;
            HitsoundLayers = hitsoundLayers;
            ProjectPath = projectpath;
            SetDefaultPaths(projectpath);
        }
    }
}

using Mapping_Tools.Classes.HitsoundStuff;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SystemTools.HitsoundMaker {
    public class Project {
        public String ProjectPath;
        public string BaseBeatmap { get; set; }
        public Sample DefaultSample { get; set; }
        public ObservableCollection<HitsoundLayer> HitsoundLayers { get; set; }

        public Project(String projectpath) {
            BaseBeatmap = "";
            DefaultSample = new Sample(0, 0, "", int.MaxValue);
            HitsoundLayers = new ObservableCollection<HitsoundLayer>();
            ProjectPath = projectpath;
        }

        public Project(string baseBeatmap, Sample defaultSample, ObservableCollection<HitsoundLayer> hitsoundLayers, String projectpath) {
            BaseBeatmap = baseBeatmap;
            DefaultSample = defaultSample;
            HitsoundLayers = hitsoundLayers;
            ProjectPath = projectpath;
        }
    }
}

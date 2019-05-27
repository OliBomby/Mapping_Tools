using Mapping_Tools.Classes.HitsoundStuff;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Viewmodels {
    public class HitsoundMakerVM : INotifyPropertyChanged {
        private string baseBeatmap;
        public string BaseBeatmap {
            get { return baseBeatmap; }
            set {
                if (baseBeatmap != value) {
                    baseBeatmap = value;
                    NotifyPropertyChanged("BaseBeatmap");
                }
            }
        }

        private Sample defaultSample;
        public Sample DefaultSample {
            get { return defaultSample; }
            set {
                if (defaultSample != value) {
                    defaultSample = value;
                    NotifyPropertyChanged("DefaultSample");
                }
            }
        }

        public ObservableCollection<HitsoundLayer> HitsoundLayers { get; set; }

        public string EditTimes { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public HitsoundMakerVM() {
            BaseBeatmap = "";
            DefaultSample = new Sample(0, 0, "", int.MaxValue);
            HitsoundLayers = new ObservableCollection<HitsoundLayer>();
        }

        public HitsoundMakerVM(string baseBeatmap, Sample defaultSample, ObservableCollection<HitsoundLayer> hitsoundLayers) {
            BaseBeatmap = baseBeatmap;
            DefaultSample = defaultSample;
            HitsoundLayers = hitsoundLayers;
        }
    }
}

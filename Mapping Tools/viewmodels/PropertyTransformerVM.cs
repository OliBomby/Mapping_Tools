using System.ComponentModel;

namespace Mapping_Tools.Viewmodels {
    public class PropertyTransformerVM : INotifyPropertyChanged{
        private double timingpointOffsetMultiplier;
        public double TimingpointOffsetMultiplier {
            get { return timingpointOffsetMultiplier; }
            set {
                if (timingpointOffsetMultiplier != value) {
                    timingpointOffsetMultiplier = value;
                    NotifyPropertyChanged("TimingpointOffsetMultiplier");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public PropertyTransformerVM() {
            TimingpointOffsetMultiplier = 134;
        }
    }
}

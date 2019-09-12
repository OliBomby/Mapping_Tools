using System.Windows.Media;
using Mapping_Tools.Classes.SystemTools.OverlaySettings;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mapping_Tools.Classes.SnappingTools {
    public class SnappingToolsSettings : INotifyPropertyChanged {
        private RelevantPointSettings _pointSettings;
        public RelevantPointSettings PointSettings {
            get => _pointSettings;
            set {
                if (_pointSettings == value) return;
                _pointSettings = value;
                OnPropertyChanged();
            }
        }

        private RelevantObjectSettings _lineSettings;
        public RelevantObjectSettings LineSettings {
            get => _lineSettings;
            set {
                if (_lineSettings == value) return;
                _lineSettings = value;
                OnPropertyChanged();
            }
        }

        private RelevantObjectSettings _circleSettings;
        public RelevantObjectSettings CircleSettings {
            get => _circleSettings;
            set {
                if (_circleSettings == value) return;
                _circleSettings = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

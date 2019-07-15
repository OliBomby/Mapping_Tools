using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff
{
    public class HitsoundZone : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _name;
        private string _filename;
        private double _xPos;
        private double _yPos;
        private SampleSet _sampleSet;
        private Hitsound _hitsound;

        public HitsoundZone() {
            IsSelected = false;
            Name = "";
            Filename = "";
            XPos = -1;
            YPos = -1;
            SampleSet = SampleSet.Auto;
            Hitsound = Hitsound.Normal;
        }

        public double Distance(Vector2 pos) {
            double dx = XPos == -1 ? 0 : XPos - pos.X;
            double dy = YPos == -1 ? 0 : YPos - pos.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public bool IsSelected {
            get { return _isSelected; }
            set {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Filename {
            get { return _filename; }
            set {
                if (_filename == value) return;
                _filename = value;
                OnPropertyChanged();
            }
        }

        public string Name {
            get { return _name; }
            set {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public double XPos {
            get { return _xPos; }
            set {
                if (_xPos == value) return;
                _xPos = value;
                OnPropertyChanged();
            }
        }

        public double YPos {
            get { return _yPos; }
            set {
                if (_yPos == value) return;
                _yPos = value;
                OnPropertyChanged();
            }
        }

        public SampleSet SampleSet {
            get { return _sampleSet; }
            set {
                if (_sampleSet == value) return;
                _sampleSet = value;
                OnPropertyChanged();
            }
        }

        public Hitsound Hitsound {
            get { return _hitsound; }
            set {
                if (_hitsound == value) return;
                _hitsound = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

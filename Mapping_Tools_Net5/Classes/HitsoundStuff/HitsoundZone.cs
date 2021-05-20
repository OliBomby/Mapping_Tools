using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// 
    /// </summary>
    public class HitsoundZone : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _name;
        private string _filename;
        private double _xPos;
        private double _yPos;
        private Hitsound _hitsound;
        private SampleSet _sampleSet;
        private SampleSet _additionsSet;
        private int _customIndex;

        public HitsoundZone() {
            _isSelected = false;
            _name = "";
            _filename = "";
            _xPos = -1;
            _yPos = -1;
            _hitsound = Hitsound.Normal;
            _sampleSet = SampleSet.Auto;
            _additionsSet = SampleSet.Auto;
            _customIndex = 0;
        }

        public HitsoundZone(bool isSelected, string name, string filename, double xPos, double yPos, Hitsound hitsound, SampleSet sampleSet, SampleSet additionsSet, int customIndex) {
            _isSelected = isSelected;
            _name = name;
            _filename = filename;
            _xPos = xPos;
            _yPos = yPos;
            _hitsound = hitsound;
            _sampleSet = sampleSet;
            _additionsSet = additionsSet;
            _customIndex = customIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public double Distance(Vector2 pos) {
            double dx = XPos == -1 ? 0 : XPos - pos.X;
            double dy = YPos == -1 ? 0 : YPos - pos.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public HitsoundZone Copy() {
            return new HitsoundZone(IsSelected, Name, Filename, XPos, YPos, Hitsound, SampleSet, AdditionsSet, CustomIndex);
        }

        public bool IsSelected {
            get => _isSelected;
            set {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Filename {
            get => _filename;
            set {
                if (_filename == value) return;
                _filename = value;
                OnPropertyChanged();
            }
        }

        public string Name {
            get => _name;
            set {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public double XPos {
            get => _xPos;
            set {
                if (_xPos == value) return;
                _xPos = value;
                OnPropertyChanged();
            }
        }

        public double YPos {
            get => _yPos;
            set {
                if (_yPos == value) return;
                _yPos = value;
                OnPropertyChanged();
            }
        }

        public Hitsound Hitsound {
            get => _hitsound;
            set {
                if (_hitsound == value) return;
                _hitsound = value;
                OnPropertyChanged();
            }
        }

        public SampleSet SampleSet {
            get => _sampleSet;
            set {
                if (_sampleSet == value) return;
                _sampleSet = value;
                OnPropertyChanged();
            }
        }

        public SampleSet AdditionsSet {
            get => _additionsSet;
            set {
                if (_additionsSet == value) return;
                _additionsSet = value;
                OnPropertyChanged();
            }
        }

        public int CustomIndex {
            get => _customIndex;
            set {
                if (_customIndex == value) return;
                _customIndex = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

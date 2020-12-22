using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.Tools.HitsoundPreviewHelper
{
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
        private SampleSet _sampleSet;
        private Hitsound _hitsound;

        /// <inheritdoc />
        public HitsoundZone() {
            _isSelected = false;
            _name = "";
            _filename = "";
            _xPos = -1;
            _yPos = -1;
            _sampleSet = SampleSet.Auto;
            _hitsound = Hitsound.Normal;
        }

        /// <inheritdoc />
        public HitsoundZone(bool isSelected, string name, string filename, double xPos, double yPos, SampleSet sampleSet, Hitsound hitsound) {
            _isSelected = isSelected;
            _name = name;
            _filename = filename;
            _xPos = xPos;
            _yPos = yPos;
            _sampleSet = sampleSet;
            _hitsound = hitsound;
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
            return new HitsoundZone(IsSelected, Name, Filename, XPos, YPos, SampleSet, Hitsound);
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

        public SampleSet SampleSet {
            get => _sampleSet;
            set {
                if (_sampleSet == value) return;
                _sampleSet = value;
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

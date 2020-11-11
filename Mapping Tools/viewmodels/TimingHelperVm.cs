using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class TimingHelperVm : BindableBase {
        #region Properties

        private bool _objects;
        private bool _bookmarks;
        private bool _greenlines;
        private bool _redlines;
        private bool _omitBarline;
        private double _leniency;
        private double _beatsBetween;
        private IBeatDivisor[] _beatDivisors;

        [JsonIgnore]
        public string[] Paths;

        [JsonIgnore]
        public bool Quick;

        public bool Objects {
            get => _objects;
            set => Set(ref _objects, value);
        }

        public bool Bookmarks {
            get => _bookmarks;
            set => Set(ref _bookmarks, value);
        }

        public bool Greenlines {
            get => _greenlines;
            set => Set(ref _greenlines, value);
        }

        public bool Redlines {
            get => _redlines;
            set => Set(ref _redlines, value);
        }

        public bool OmitBarline {
            get => _omitBarline;
            set => Set(ref _omitBarline, value);
        }

        public double Leniency {
            get => _leniency;
            set => Set(ref _leniency, value);
        }

        public double BeatsBetween {
            get => _beatsBetween;
            set => Set(ref _beatsBetween, value);
        }

        public IBeatDivisor[] BeatDivisors {
            get => _beatDivisors;
            set => Set(ref _beatDivisors, value);
        }

        #endregion

        public TimingHelperVm() {
            Objects = true;
            Bookmarks = true;
            Greenlines = true;
            Redlines = true;
            OmitBarline = false;
            Leniency = 3;
            BeatsBetween = -1;
            BeatDivisors = RationalBeatDivisor.GetDefaultBeatDivisors();
        }
    }
}
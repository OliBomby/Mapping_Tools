using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class TimingHelperVm : BindableBase {
        #region Properties

        private bool objects;
        private bool bookmarks;
        private bool greenlines;
        private bool redlines;
        private bool omitBarline;
        private double leniency;
        private double beatsBetween;
        private IBeatDivisor[] beatDivisors;

        [JsonIgnore]
        public string[] Paths;

        [JsonIgnore]
        public bool Quick;

        public bool Objects {
            get => objects;
            set => Set(ref objects, value);
        }

        public bool Bookmarks {
            get => bookmarks;
            set => Set(ref bookmarks, value);
        }

        public bool Greenlines {
            get => greenlines;
            set => Set(ref greenlines, value);
        }

        public bool Redlines {
            get => redlines;
            set => Set(ref redlines, value);
        }

        public bool OmitBarline {
            get => omitBarline;
            set => Set(ref omitBarline, value);
        }

        public double Leniency {
            get => leniency;
            set => Set(ref leniency, value);
        }

        public double BeatsBetween {
            get => beatsBetween;
            set => Set(ref beatsBetween, value);
        }

        public IBeatDivisor[] BeatDivisors {
            get => beatDivisors;
            set => Set(ref beatDivisors, value);
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
using System;
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

        private double timingpointOffsetOffset;
        public double TimingpointOffsetOffset {
            get { return timingpointOffsetOffset; }
            set {
                if (timingpointOffsetOffset != value) {
                    timingpointOffsetOffset = value;
                    NotifyPropertyChanged("TimingpointOffsetOffset");
                }
            }
        }

        private double timingpointBPMMultiplier;
        public double TimingpointBPMMultiplier {
            get { return timingpointBPMMultiplier; }
            set {
                if (timingpointBPMMultiplier != value) {
                    timingpointBPMMultiplier = value;
                    NotifyPropertyChanged("TimingpointBPMMultiplier");
                }
            }
        }

        private double timingpointBPMOffset;
        public double TimingpointBPMOffset {
            get { return timingpointBPMOffset; }
            set {
                if (timingpointBPMOffset != value) {
                    timingpointBPMOffset = value;
                    NotifyPropertyChanged("TimingpointBPMOffset");
                }
            }
        }

        private double timingpointSVMultiplier;
        public double TimingpointSVMultiplier {
            get { return timingpointSVMultiplier; }
            set {
                if (timingpointSVMultiplier != value) {
                    timingpointSVMultiplier = value;
                    NotifyPropertyChanged("TimingpointSVMultiplier");
                }
            }
        }

        private double timingpointSVOffset;
        public double TimingpointSVOffset {
            get { return timingpointSVOffset; }
            set {
                if (timingpointSVOffset != value) {
                    timingpointSVOffset = value;
                    NotifyPropertyChanged("TimingpointSVOffset");
                }
            }
        }

        private double timingpointIndexMultiplier;
        public double TimingpointIndexMultiplier {
            get { return timingpointIndexMultiplier; }
            set {
                if (timingpointIndexMultiplier != value) {
                    timingpointIndexMultiplier = value;
                    NotifyPropertyChanged("TimingpointIndexMultiplier");
                }
            }
        }

        private double timingpointIndexOffset;
        public double TimingpointIndexOffset {
            get { return timingpointIndexOffset; }
            set {
                if (timingpointIndexOffset != value) {
                    timingpointIndexOffset = value;
                    NotifyPropertyChanged("TimingpointIndexOffset");
                }
            }
        }

        private double timingpointVolumeMultiplier;
        public double TimingpointVolumeMultiplier {
            get { return timingpointVolumeMultiplier; }
            set {
                if (timingpointVolumeMultiplier != value) {
                    timingpointVolumeMultiplier = value;
                    NotifyPropertyChanged("TimingpointVolumeMultiplier");
                }
            }
        }

        private double timingpointVolumeOffset;
        public double TimingpointVolumeOffset {
            get { return timingpointVolumeOffset; }
            set {
                if (timingpointVolumeOffset != value) {
                    timingpointVolumeOffset = value;
                    NotifyPropertyChanged("TimingpointVolumeOffset");
                }
            }
        }

        private double hitObjectTimeMultiplier;
        public double HitObjectTimeMultiplier {
            get { return hitObjectTimeMultiplier; }
            set {
                if (hitObjectTimeMultiplier != value) {
                    hitObjectTimeMultiplier = value;
                    NotifyPropertyChanged("HitObjectTimeMultiplier");
                }
            }
        }

        private double hitObjectTimeOffset;
        public double HitObjectTimeOffset {
            get { return hitObjectTimeOffset; }
            set {
                if (hitObjectTimeOffset != value) {
                    hitObjectTimeOffset = value;
                    NotifyPropertyChanged("HitObjectTimeOffset");
                }
            }
        }

        private double bookmarkTimeMultiplier;
        public double BookmarkTimeMultiplier {
            get { return bookmarkTimeMultiplier; }
            set {
                if (bookmarkTimeMultiplier != value) {
                    bookmarkTimeMultiplier = value;
                    NotifyPropertyChanged("BookmarkTimeMultiplier");
                }
            }
        }

        private double bookmarkTimeOffset;
        public double BookmarkTimeOffset {
            get { return bookmarkTimeOffset; }
            set {
                if (bookmarkTimeOffset != value) {
                    bookmarkTimeOffset = value;
                    NotifyPropertyChanged("BookmarkTimeOffset");
                }
            }
        }

        private double sbSampleTimeMultiplier;
        public double SBSampleTimeMultiplier {
            get { return sbSampleTimeMultiplier; }
            set {
                if (sbSampleTimeMultiplier != value) {
                    sbSampleTimeMultiplier = value;
                    NotifyPropertyChanged("SBSampleTimeMultiplier");
                }
            }
        }

        private double sbSampleTimeOffset;
        public double SBSampleTimeOffset {
            get { return sbSampleTimeOffset; }
            set {
                if (sbSampleTimeOffset != value) {
                    sbSampleTimeOffset = value;
                    NotifyPropertyChanged("SBSampleTimeOffset");
                }
            }
        }

        private bool clipProperties;
        public bool ClipProperties {
            get { return clipProperties; }
            set {
                if (clipProperties != value) {
                    clipProperties = value;
                    NotifyPropertyChanged("ClipProperties");
                }
            }
        }

        private bool enableFilters;
        public bool EnableFilters {
            get { return enableFilters; }
            set {
                if (enableFilters != value) {
                    enableFilters = value;
                    NotifyPropertyChanged("EnableFilters");
                }
            }
        }

        private double matchFilter;
        public double MatchFilter {
            get { return matchFilter; }
            set {
                if (matchFilter != value) {
                    matchFilter = value;
                    NotifyPropertyChanged("MatchFilter");
                }
            }
        }

        private double minTimeFilter;
        public double MinTimeFilter {
            get { return minTimeFilter; }
            set {
                if (minTimeFilter != value) {
                    minTimeFilter = value;
                    NotifyPropertyChanged("MinTimeFilter");
                }
            }
        }

        private double maxTimeFilter;
        public double MaxTimeFilter {
            get { return maxTimeFilter; }
            set {
                if (maxTimeFilter != value) {
                    maxTimeFilter = value;
                    NotifyPropertyChanged("MaxTimeFilter");
                }
            }
        }

        public string[] MapPaths;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public PropertyTransformerVM() {
            TimingpointOffsetMultiplier = 1;
            TimingpointOffsetOffset = 0;
            TimingpointBPMMultiplier = 1;
            TimingpointBPMOffset = 0;
            TimingpointSVMultiplier = 1;
            TimingpointSVOffset = 0;
            TimingpointIndexMultiplier = 1;
            TimingpointIndexOffset = 0;
            TimingpointVolumeMultiplier = 1;
            TimingpointVolumeOffset = 0;
            HitObjectTimeMultiplier = 1;
            HitObjectTimeOffset = 0;
            BookmarkTimeMultiplier = 1;
            BookmarkTimeOffset = 0;
            SBSampleTimeMultiplier = 1;
            SBSampleTimeOffset = 0;

            ClipProperties = false;
            EnableFilters = false;
            MatchFilter = -1;
            MinTimeFilter = -1;
            MaxTimeFilter = -1;
        }
    }
}

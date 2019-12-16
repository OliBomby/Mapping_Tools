using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Viewmodels {
    public class SlideratorVm : BindableBase {
        private ObservableCollection<HitObject> _loadedHitObjects;
        private HitObject _visibleHitObject;
        private double _graphBeats;
        private double _beatsPerMinute;
        private int _beatSnapDivisor;
        private TimeSpan _graphDuration;

        public ObservableCollection<HitObject> LoadedHitObjects {
            get => _loadedHitObjects;
            set => Set(ref _loadedHitObjects, value);
        }

        public HitObject VisibleHitObject {
            get => _visibleHitObject;
            set => Set(ref _visibleHitObject, value);
        }

        public double GraphBeats {
            get => _graphBeats;
            set {
                if (Set(ref _graphBeats, value)) {
                    UpdateAnimationDuration();
                }
            }
        }

        public double BeatsPerMinute {
            get => _beatsPerMinute;
            set {
                if (Set(ref _beatsPerMinute, value)) {
                    UpdateAnimationDuration();
                }
            } 
        }

        public int BeatSnapDivisor {
            get => _beatSnapDivisor;
            set => Set(ref _beatSnapDivisor, value);
        }

        public TimeSpan GraphDuration {
            get => _graphDuration;
            set => Set(ref _graphDuration, value);
        }

        public SlideratorVm() {
            LoadedHitObjects = new ObservableCollection<HitObject>();
            BeatsPerMinute = 180;
            GraphBeats = 3;
            BeatSnapDivisor = 4;
        }

        private void UpdateAnimationDuration() {
            if (BeatsPerMinute < 1) return;
            GraphDuration = TimeSpan.FromMinutes(GraphBeats / BeatsPerMinute);
        }
    }
}
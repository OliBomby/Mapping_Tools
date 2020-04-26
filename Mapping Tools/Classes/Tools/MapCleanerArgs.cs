using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.Tools {
    public class MapCleanerArgs : BindableBase {
        private bool _volumeSliders;
        private bool _sampleSetSliders;
        private bool _volumeSpinners;
        private bool _resnapObjects;
        private bool _resnapBookmarks;
        private bool _removeUnusedSamples;
        private bool _removeMuting;
        private bool _removeUnclickableHitsounds;
        private int _snap1;
        private int _snap2;

        public bool VolumeSliders {
            get => _volumeSliders;
            set => Set(ref _volumeSliders, value);
        }

        public bool SampleSetSliders {
            get => _sampleSetSliders;
            set => Set(ref _sampleSetSliders, value);
        }

        public bool VolumeSpinners {
            get => _volumeSpinners;
            set => Set(ref _volumeSpinners, value);
        }

        public bool ResnapObjects {
            get => _resnapObjects;
            set => Set(ref _resnapObjects, value);
        }

        public bool ResnapBookmarks {
            get => _resnapBookmarks;
            set => Set(ref _resnapBookmarks, value);
        }

        public bool RemoveUnusedSamples {
            get => _removeUnusedSamples;
            set => Set(ref _removeUnusedSamples, value);
        }

        public bool RemoveMuting {
            get => _removeMuting;
            set => Set(ref _removeMuting, value);
        }

        public bool RemoveUnclickableHitsounds {
            get => _removeUnclickableHitsounds;
            set => Set(ref _removeUnclickableHitsounds, value);
        }

        public int Snap1 {
            get => _snap1;
            set => Set(ref _snap1, value);
        }

        public int Snap2 {
            get => _snap2;
            set => Set(ref _snap2, value);
        }

        public MapCleanerArgs(bool volumeSliders, bool sampleSetSliders, bool volumeSpinners, bool resnapObjects, bool resnapBookmarks, bool removeUnusedSamples, bool removeMuting, bool removeUnclickableHitsounds, int snap1, int snap2) {
            _volumeSliders = volumeSliders;
            _sampleSetSliders = sampleSetSliders;
            _volumeSpinners = volumeSpinners;
            _resnapObjects = resnapObjects;
            _resnapBookmarks = resnapBookmarks;
            _removeUnusedSamples = removeUnusedSamples;
            _removeMuting = removeMuting;
            _removeUnclickableHitsounds = removeUnclickableHitsounds;
            _snap1 = snap1;
            _snap2 = snap2;
        }

        public static readonly MapCleanerArgs BasicClean = new MapCleanerArgs(true, true, true, false, false, false, false, false, 16, 12);

        public static readonly MapCleanerArgs BasicResnap = new MapCleanerArgs(true, true, true, true, false, false, false, false, 16, 12);
    }
}
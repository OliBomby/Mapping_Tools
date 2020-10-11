using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mapping_Tools.Classes.BeatmapHelper;
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
        private ObservableCollection<IBeatDivisor> _beatDivisors;

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

        public ObservableCollection<IBeatDivisor> BeatDivisors {
            get => _beatDivisors;
            set => Set(ref _beatDivisors, value);
        }

        public MapCleanerArgs(bool volumeSliders, bool sampleSetSliders, bool volumeSpinners, bool resnapObjects, bool resnapBookmarks, bool removeUnusedSamples, bool removeMuting, bool removeUnclickableHitsounds, IEnumerable<IBeatDivisor> beatDivisors) {
            _volumeSliders = volumeSliders;
            _sampleSetSliders = sampleSetSliders;
            _volumeSpinners = volumeSpinners;
            _resnapObjects = resnapObjects;
            _resnapBookmarks = resnapBookmarks;
            _removeUnusedSamples = removeUnusedSamples;
            _removeMuting = removeMuting;
            _removeUnclickableHitsounds = removeUnclickableHitsounds;
            _beatDivisors = new ObservableCollection<IBeatDivisor>(beatDivisors);
        }

        public static readonly MapCleanerArgs BasicClean = new MapCleanerArgs(true, true, true, false, false, false, false, false, new RationalBeatDivisor[] { 16, 12 });

        public static readonly MapCleanerArgs BasicResnap = new MapCleanerArgs(true, true, true, true, false, false, false, false, new RationalBeatDivisor[] { 16, 12 });
    }
}
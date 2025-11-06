using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.Tools.MapCleanerStuff {
    public class MapCleanerArgs : BindableBase {
        private bool volumeSliders;
        private bool sampleSetSliders;
        private bool volumeSpinners;
        private bool resnapObjects;
        private bool resnapBookmarks;
        private bool analyzeSamples;
        private bool removeUnusedSamples;
        private bool removeHitsounds;
        private bool removeMuting;
        private bool removeUnclickableHitsounds;
        private IBeatDivisor[] beatDivisors;

        public bool VolumeSliders {
            get => volumeSliders;
            set => Set(ref volumeSliders, value);
        }

        public bool SampleSetSliders {
            get => sampleSetSliders;
            set => Set(ref sampleSetSliders, value);
        }

        public bool VolumeSpinners {
            get => volumeSpinners;
            set => Set(ref volumeSpinners, value);
        }

        public bool ResnapObjects {
            get => resnapObjects;
            set => Set(ref resnapObjects, value);
        }

        public bool ResnapBookmarks {
            get => resnapBookmarks;
            set => Set(ref resnapBookmarks, value);
        }

        public bool AnalyzeSamples {
            get => analyzeSamples;
            set => Set(ref analyzeSamples, value);
        }

        public bool RemoveUnusedSamples {
            get => removeUnusedSamples;
            set => Set(ref removeUnusedSamples, value);
        }

        public bool RemoveHitsounds {
            get => removeHitsounds;
            set => Set(ref removeHitsounds, value);
        }

        public bool RemoveMuting {
            get => removeMuting;
            set => Set(ref removeMuting, value);
        }

        public bool RemoveUnclickableHitsounds {
            get => removeUnclickableHitsounds;
            set => Set(ref removeUnclickableHitsounds, value);
        }

        public IBeatDivisor[] BeatDivisors {
            get => beatDivisors;
            set => Set(ref beatDivisors, value);
        }

        public MapCleanerArgs(bool volumeSliders, bool sampleSetSliders, bool volumeSpinners, bool resnapObjects, bool resnapBookmarks, bool analyzeSamples, bool removeUnusedSamples, bool removeHitsounds, bool removeMuting, bool removeUnclickableHitsounds, IEnumerable<IBeatDivisor> beatDivisors) {
            this.volumeSliders = volumeSliders;
            this.sampleSetSliders = sampleSetSliders;
            this.volumeSpinners = volumeSpinners;
            this.resnapObjects = resnapObjects;
            this.resnapBookmarks = resnapBookmarks;
            this.analyzeSamples = analyzeSamples;
            this.removeUnusedSamples = removeUnusedSamples;
            this.removeHitsounds = removeHitsounds;
            this.removeMuting = removeMuting;
            this.removeUnclickableHitsounds = removeUnclickableHitsounds;
            this.beatDivisors = beatDivisors.ToArray();
        }

        public static readonly MapCleanerArgs BasicClean = new MapCleanerArgs(true, true, true, false, false, true, false, false, false, false, RationalBeatDivisor.GetDefaultBeatDivisors());

        public static readonly MapCleanerArgs BasicResnap = new MapCleanerArgs(true, true, true, true, false, true, false, false, false, false, RationalBeatDivisor.GetDefaultBeatDivisors());
    }
}
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools_Core.Tools.MapCleanerStuff {
    public class MapCleanerArgs : IMapCleanerArgs {
        public bool VolumeSliders { get; }
        public bool SampleSetSliders { get; }
        public bool VolumeSpinners { get; }
        public bool ResnapObjects { get; }
        public bool ResnapBookmarks { get; }
        public bool RemoveUnusedSamples { get; }
        public bool RemoveMuting { get; }
        public bool RemoveUnclickableHitsounds { get; }
        public IBeatDivisor[] BeatDivisors { get; }

        public MapCleanerArgs(bool volumeSliders, bool sampleSetSliders, bool volumeSpinners, bool resnapObjects, bool resnapBookmarks, bool removeUnusedSamples, bool removeMuting, bool removeUnclickableHitsounds, IEnumerable<IBeatDivisor> beatDivisors) {
            VolumeSliders = volumeSliders;
            SampleSetSliders = sampleSetSliders;
            VolumeSpinners = volumeSpinners;
            ResnapObjects = resnapObjects;
            ResnapBookmarks = resnapBookmarks;
            RemoveUnusedSamples = removeUnusedSamples;
            RemoveMuting = removeMuting;
            RemoveUnclickableHitsounds = removeUnclickableHitsounds;
            BeatDivisors = beatDivisors.ToArray();
        }

        public static readonly MapCleanerArgs BasicClean = new MapCleanerArgs(true, true, true, false, false, false, false, false, RationalBeatDivisor.GetDefaultBeatDivisors());

        public static readonly MapCleanerArgs BasicResnap = new MapCleanerArgs(true, true, true, true, false, false, false, false, RationalBeatDivisor.GetDefaultBeatDivisors());
    }
}
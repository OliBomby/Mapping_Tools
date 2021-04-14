using Mapping_Tools_Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools_Core.Tools.MapCleanerStuff {
    public interface IMapCleanerArgs {
        bool VolumeSliders { get; }

        bool SampleSetSliders { get; }

        bool VolumeSpinners { get; }

        bool ResnapObjects { get; }

        bool ResnapBookmarks { get; }

        bool RemoveUnusedSamples { get; }

        bool RemoveMuting { get; }

        bool RemoveUnclickableHitsounds { get; }

        IBeatDivisor[] BeatDivisors { get; }
    }
}
using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.MapCleanerStuff;

public interface IMapCleanerArgs {
    bool VolumeSliders { get; }

    bool SampleSetSliders { get; }

    bool VolumeSpinners { get; }

    bool ResnapObjects { get; }

    bool ResnapBookmarks { get; }

    bool RemoveMuting { get; }

    bool RemoveUnclickableHitsounds { get; }

    IBeatDivisor[] BeatDivisors { get; }
}
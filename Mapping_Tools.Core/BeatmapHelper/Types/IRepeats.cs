namespace Mapping_Tools.Core.BeatmapHelper.Types;

public interface IRepeats : IHasRepeats, IDuration {
    void SetRepeatCount(int repeatCount);

    void SetSpanCount(int spanCount);

    void SetSpanDuration(double spanDuration);
}
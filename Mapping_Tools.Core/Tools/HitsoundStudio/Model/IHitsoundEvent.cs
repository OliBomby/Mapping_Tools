using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.HitsoundStudio.Model;

public interface IHitsoundEvent {
    double Time { get; }
    Vector2 Pos { get; }
    double Volume { get; }
    string Filename { get; }
    SampleSet SampleSet { get; }
    SampleSet Additions { get; }
    int CustomIndex { get; }
    bool Whistle { get; }
    bool Finish { get; }
    bool Clap { get; }

    int GetHitsounds();
}
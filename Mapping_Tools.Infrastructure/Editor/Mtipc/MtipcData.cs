using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc;

internal sealed record MtipcBeatmapData(double SliderMultiplier, double SliderTickRate, double ApproachRate,
    double CircleSize, int PreviewTime, string ContainingFolder, string Filename);

internal sealed record MtipcControlPointData(double BeatLength, double Offset, int CustomSamples,
    int SampleSet, int TimeSignature, int Volume, int EffectFlags, bool TimingChange);

internal sealed record MtipcHitObjectData(double SpatialLength, int StartTime, int EndTime, int Type,
    int SoundType, int SegmentCount, Vector2 Position, string? SampleFile, int SampleVolume, int SampleSet,
    int SampleSetAdditions, int CustomSampleSet, bool IsSelected, int CurveType,
    IReadOnlyList<Vector2> CurvePoints, IReadOnlyList<int> SoundTypeList, IReadOnlyList<int> SampleSetList,
    IReadOnlyList<int> SampleSetAdditionsList);

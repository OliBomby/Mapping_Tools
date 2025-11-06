using Mapping_Tools.Domain.Beatmaps.Enums;

namespace Mapping_Tools.Domain.Beatmaps.Types;

/// <summary>
/// Interface to allow configuration of timing point decoder.
/// </summary>
public interface IConfigurableTimingPointDecoder {
    /// <summary>
    /// Default fallback value for parsing sample volume.
    /// </summary>
    double DefaultVolume { get; set; }

    /// <summary>
    /// Default fallback value for parsing sample set.
    /// </summary>
    SampleSet DefaultSampleSet { get; set; }
}
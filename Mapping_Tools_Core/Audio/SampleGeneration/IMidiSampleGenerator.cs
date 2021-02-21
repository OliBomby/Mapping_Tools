using Mapping_Tools_Core.Audio.Midi;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    /// <summary>
    /// Represents the arguments for a single MIDI note.
    /// </summary>
    public interface IMidiSampleGenerator : ISampleGenerator {
        IMidiNote Note { get; }
    }
}
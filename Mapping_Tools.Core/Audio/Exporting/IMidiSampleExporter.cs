using Mapping_Tools.Core.Audio.Midi;

namespace Mapping_Tools.Core.Audio.Exporting;

public interface IMidiSampleExporter : ISampleExporter {
    /// <summary>
    /// Adds a MIDI note to the exporter.
    /// </summary>
    /// <param name="note">The MIDI note to add</param>
    void AddMidiNote(IMidiNote note);
}
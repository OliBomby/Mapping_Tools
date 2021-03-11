using Mapping_Tools_Core.Audio.Midi;

namespace Mapping_Tools_Core.Audio.Exporting {
    public interface IMidiSampleExporter : ISampleExporter {
        /// <summary>
        /// Adds a MIDI note to the exporter.
        /// </summary>
        /// <param name="note">The MIDI note to add</param>
        void AddMidiNote(IMidiNote note);
    }
}